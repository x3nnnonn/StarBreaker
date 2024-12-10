# Most of the code is taken from https://github.com/aarnaut/mitmproxy-grpc
# Thanks! :D

# It's used to intercept and inspect grpc traffic. It uses exported descriptors from the game exe
# to deserialize grpc bytes into readable text.

# It also decides which requests and responses to stream based on their rpc descriptors.


import datetime
import os
import typing
import mitmproxy
from google.protobuf.descriptor_pb2 import FileDescriptorSet
from google.protobuf.descriptor import MethodDescriptor
from google.protobuf.descriptor_pool import DescriptorPool
from google.protobuf.message_factory import MessageFactory
from google.protobuf.message import DecodeError
from google.protobuf.text_format import MessageToString


class ProtobufModifier:
    """
    Wrapper around google protobuf package that provides serialization and deserialization of protobuf content.
    The implementation uses a proto descriptor to resolve messages. Method resolution works based on HTTP path.

    NOTE: Content compression is not supported.
    """

    def __init__(self) -> None:
        self.descriptor_pool = DescriptorPool()

    def set_descriptor(self, descriptor_path: str) -> None:
        with open(descriptor_path, mode="rb") as file:
            descriptor = FileDescriptorSet.FromString(file.read())
            for proto in descriptor.file:
                self.descriptor_pool.Add(proto)

            self.message_factory = MessageFactory(self.descriptor_pool)

    def deserialize(
            self,
            http_message: mitmproxy.http.Message,
            path: str,
            serialized_protobuf: bytes,
    ) -> str:
        """
        Takes a protobuf byte array and returns a deserialized string in text format.
        You must set a descriptor file must prior to calling this method.
        The string is formatted according to `google.protobuf.text_format`

        Raises:
            ValueError - in case deserialization fails because the method could not be resolved or the input data is invalid.
        """

        grpc_method = self.find_method_by_path(path)
        # Strip the length and compression header; 5 bytes in total.
        # Payload compression is not supported at the moment.
        data_without_prefix = serialized_protobuf[5:]

        if isinstance(http_message, mitmproxy.http.Request):
            message = self.message_factory.GetPrototype(grpc_method.input_type)()
        elif isinstance(http_message, mitmproxy.http.Response):
            message = self.message_factory.GetPrototype(grpc_method.output_type)()
        else:
            raise ValueError(f"Unexpected HTTP message type {http_message}")

        message.Clear()

        try:
            message.MergeFromString(data_without_prefix)
        except DecodeError as e:
            raise ValueError("Unable to deserialize input") from e

        return MessageToString(
            message=message,
            descriptor_pool=self.descriptor_pool,
            print_unknown_fields=True,
        )

    def find_method_by_path(self, path: str) -> MethodDescriptor:
        try:
            # Drop the first '/' from the path and convert the rest to a fully qualified namespace that we can look up.
            method_path = path.replace("/", ".")[1:]
            return self.descriptor_pool.FindMethodByName(method_path)
        except KeyError as e:
            raise ValueError("Failed to resolve method name by path") from e


class GrpcOption:

    def __init__(self, protobuf_modifier: ProtobufModifier) -> None:
        self.protobuf_modifier = protobuf_modifier

    def load(self, loader):
        loader.add_option(
            name="descriptor",
            typespec=typing.Optional[str],
            default=None,
            help="Set the descriptor file used for serialiation and deserialization of protobuf content",
        )

    def configure(self, updates):
        if (
                "descriptor" in updates
                and mitmproxy.ctx.options.__contains__("descriptor")
                and mitmproxy.ctx.options.descriptor is not None
        ):
            self.protobuf_modifier.set_descriptor(mitmproxy.ctx.options.descriptor)


class GrpcProtobufContentView(mitmproxy.contentviews.base.View):

    name = "gRPC/Protocol Buffer using protoc"

    supported_content_types = ["application/grpc", "application/grpc+proto"]

    def __init__(self, protobuf_modifier: ProtobufModifier) -> None:
        self.protobuf_modifier = protobuf_modifier

    def __call__(
            self,
            data: bytes,
            *,
            content_type: typing.Optional[str] = None,
            flow: typing.Optional[mitmproxy.flow.Flow] = None,
            http_message: typing.Optional[mitmproxy.http.Message] = None,
            **unknown_metadata,
    ):
        deserialized = self.protobuf_modifier.deserialize(
            http_message, flow.request.path, data
        )
        return self.name, mitmproxy.contentviews.base.format_text(deserialized)

    def render_priority(
            self, data: bytes, *, content_type: typing.Optional[str] = None, **metadata
    ) -> float:
        return float(content_type in self.supported_content_types)


class GrpcProtobufDebugWriter:

    def __write(self, req_or_rep, path, content):
        if not os.path.exists("dump"):
            os.makedirs("dump")

        with open(
                os.path.join(
                    "dump",
                    datetime.datetime.now().strftime("%Y%m%d.%H%M%S.%f")
                    + "-"
                    + req_or_rep
                    + "-"
                    + path
                    + ".grpc",
                ),
                "wb",
        ) as f:
            f.write(content.encode())

    def __init__(self, protobuf_modifier: ProtobufModifier) -> None:
        self.protobuf_modifier = protobuf_modifier

    def request(self, flow: mitmproxy.http.HTTPFlow):
        req = self.protobuf_modifier.deserialize(
            flow.request, flow.request.path, flow.request.content
        )
        path = flow.request.path.replace("/", "_")
        self.__write("request", path, req)

    def response(self, flow: mitmproxy.http.HTTPFlow):
        rep = self.protobuf_modifier.deserialize(
            flow.response, flow.request.path, flow.response.content
        )
        path = flow.request.path.replace("/", "_")
        self.__write("response", path, rep)


protobuf_modifier = ProtobufModifier()
contentView = GrpcProtobufContentView(protobuf_modifier)


def load(loader):
    mitmproxy.contentviews.add(contentView)


def done():
    mitmproxy.contentviews.remove(contentView)


def requestheaders(flow: mitmproxy.http.HTTPFlow):
    target_type = protobuf_modifier.find_method_by_path(flow.request.path)
    if target_type is not None:
        flow.request.stream = target_type.client_streaming
    else:
        flow.request.stream = False


def responseheaders(flow: mitmproxy.http.HTTPFlow):
    target_type = protobuf_modifier.find_method_by_path(flow.request.path)
    if target_type is not None:
        flow.response.stream = target_type.server_streaming
    else:
        flow.response.stream = False


addons = [GrpcOption(protobuf_modifier), GrpcProtobufDebugWriter(protobuf_modifier)]
