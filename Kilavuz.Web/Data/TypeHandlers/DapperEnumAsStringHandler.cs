using System;
using System.Data;
using Dapper;

namespace Kilavuz.Web.Data.TypeHandlers;

public class DapperEnumAsStringHandler<TEnum> : SqlMapper.TypeHandler<TEnum> where TEnum : struct, Enum
{
    public override void SetValue(IDbDataParameter parameter, TEnum value)
    {
        parameter.Value = value.ToString();
    }

    public override TEnum Parse(object value)
    {
        if (value == null || value is DBNull)
            return default;

        var str = value.ToString();
        if (Enum.TryParse<TEnum>(str, true, out var result))
        {
            return result;
        }

        return default;
    }
}
