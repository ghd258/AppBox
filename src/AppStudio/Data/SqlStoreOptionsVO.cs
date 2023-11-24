using System.Collections.Generic;
using System.Linq.Expressions;
using AppBoxCore;

namespace AppBoxDesign;

public sealed class SqlStoreOptionsVO
{
    public long StoreModelId { get; private set; }

    public IList<OrderedField> PrimaryKeys { get; private set; } = null!;

    public IList<SqlIndexModelVO> Indexes { get; private set; } = null!;

#if __APPBOXDESIGN__
    public static SqlStoreOptionsVO From(SqlStoreOptions options)
    {
        var vo = new SqlStoreOptionsVO();
        vo.StoreModelId = options.StoreModelId;
        vo.PrimaryKeys = options.HasPrimaryKeys
            ? new List<OrderedField>(options.PrimaryKeys)
            : new List<OrderedField>();
        vo.Indexes = new List<SqlIndexModelVO>();
        if (options.HasIndexes)
        {
            foreach (var indexModel in options.Indexes)
            {
                vo.Indexes.Add(SqlIndexModelVO.From(indexModel));
            }
        }

        return vo;
    }

    internal void WriteTo(IOutputStream ws)
    {
        ws.WriteLong(StoreModelId);
        ws.WriteVariant(PrimaryKeys.Count);
        for (var i = 0; i < PrimaryKeys.Count; i++)
        {
            PrimaryKeys[i].WriteTo(ws);
        }
        ws.WriteVariant(Indexes.Count);
        for (var i = 0; i < Indexes.Count; i++)
        {
            Indexes[i].WriteTo(ws);
        }
    }

#else
    internal void ReadFrom(IInputStream rs)
    {
        StoreModelId = rs.ReadLong();
        var pkCount = rs.ReadVariant();
        PrimaryKeys = new List<OrderedField>();
        for (var i = 0; i < pkCount; i++)
        {
            var pk = new OrderedField();
            pk.ReadFrom(rs);
            PrimaryKeys.Add(pk);
        }

        var idxCount = rs.ReadVariant();
        Indexes = new List<SqlIndexModelVO>();
        for (var i = 0; i < idxCount; i++)
        {
            var idx = new SqlIndexModelVO();
            idx.ReadFrom(rs);
            Indexes.Add(idx);
        }
    }

#endif
}