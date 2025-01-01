using Pfim;
using StarBreaker.Dds;

namespace StarBreaker.Sandbox;

public static class DdsUnsplit
{
    public static void Run()
    {
        Merge();
    }

    private static void Convert()
    {
        var things = Directory.GetFiles(@"D:\StarCitizen\P4k", "*.dds", SearchOption.AllDirectories);
        Parallel.ForEach(things, file =>
        {
            Console.WriteLine(file);
            try
            {
                using var mergedDds = DdsFile.MergeToStream(file);
                using var image = Pfimage.FromStream(mergedDds);
                image.SaveAsPng(file + ".png");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        });
    }

    private static void Merge()
    {
        var things = Directory.GetFiles(@"D:\StarCitizen\P4k", "*.dds", SearchOption.AllDirectories);
        Parallel.ForEach(things, file =>
        {
            var target = Path.ChangeExtension(file, ".full.dds");
            if (File.Exists(target))
                return;

            Console.WriteLine(file);
            try
            {
                DdsFile.MergeToFile(file, target);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        });
    }
}