using System.Runtime.CompilerServices;

namespace StarBreaker.Forge;

public readonly ref struct DataForgePropertyEnumerator
{
    private readonly Enumerator _enumerator;
    
    public DataForgePropertyEnumerator(DataForgeStructDefinition def, Database database)
    {
        _enumerator = new Enumerator(def, database);
    }
    
    public Enumerator GetEnumerator() => _enumerator;

    public ref struct Enumerator
    {
        private readonly ReadOnlySpan<DataForgePropertyDefinition> _properties;
        private readonly ParentStructDefinitions _parents;
        private int _currentParent;
        private int _index;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator(DataForgeStructDefinition structDef, Database _database)
        {
            _properties = _database.PropertyDefinitions.Span;
            var structs = _database.StructDefinitions.Span;
            
            _parents = new ParentStructDefinitions();
            Span<DataForgeStructDefinition> parents = _parents;
            var count = 0;
            ref readonly var baseStruct = ref structDef;
            do
            {
                if (baseStruct.AttributeCount > 0)
                {
                    parents[count++] = baseStruct;
                    
                    if (count == parents.Length)
                        throw new Exception("Too many levels");
                }

                if (baseStruct.ParentTypeIndex == 0xFFFFFFFF)
                    break;

                baseStruct = ref structs[(int)baseStruct.ParentTypeIndex];
            } while (true);

            _currentParent = count - 1;

            if (count != 0)
            {
                _index = parents[_currentParent].FirstAttributeIndex - 1;
            }
            else
            {
                _index = -1;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            ReadOnlySpan<DataForgeStructDefinition> parents = _parents;
            
            // If we've iterated through all parents, return false.
            if (_currentParent < 0)
                return false;

            // Increment the index to move to the next property.
            ++_index;

            // Check if the current index exceeds the range of the current parent's properties.
            // If it does, move to the next parent (if any) and update the index to the start of its properties.
            while (_currentParent >= 0 && _index >= parents[_currentParent].FirstAttributeIndex + parents[_currentParent].AttributeCount)
            {
                --_currentParent; // Move to the next parent.
                if (_currentParent >= 0)
                    _index = parents[_currentParent].FirstAttributeIndex; // Update index to the start of the next parent's properties.
                else
                    return false; // If there are no more parents, stop iteration.
            }

            return _currentParent >= 0; // Return true if we are still within a valid parent range, false otherwise.
        }

        public ref readonly DataForgePropertyDefinition Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _properties[_index];
        }
    }

    //this is length 8 because the deepest chain I've seen is 6. We can increase this if needed.
    [InlineArray(8)]
    private struct ParentStructDefinitions
    {
        private DataForgeStructDefinition _field;
    }
}