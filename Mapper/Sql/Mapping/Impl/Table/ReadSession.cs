using Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Column;
using System;
using System.Collections.Generic;

namespace Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Table
{

    /// <summary>
    /// 
    /// </summary>
    public class ReadSession
    {
        Dictionary<Type, List<ColumnPos>> Entities = new Dictionary<Type, List<ColumnPos>>();

        public List<ColumnPos> GetColumnPositions(Type type, int size)
        {
            List<ColumnPos> positions;
            if (!Entities.TryGetValue(type, out positions))
            {
                positions = new List<ColumnPos>();
                for (var i = 0; i < size; i++)
                    positions.Add(new ColumnPos());

                Entities[type] = positions;
            }

            return positions;
        }
    }
        
}
