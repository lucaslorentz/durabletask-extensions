using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace LLL.DurableTask.EFCore.MySql;

public static class QueryableExtesions
{
    public static IQueryable<T> WithStraightJoin<T>(this IQueryable<T> queryable)
    {
        return queryable.TagWith("STRAIGHT_JOIN");
    }
}
