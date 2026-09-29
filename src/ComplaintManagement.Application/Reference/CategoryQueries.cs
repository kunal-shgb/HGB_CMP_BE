using ComplaintManagement.Domain.Entities;

namespace ComplaintManagement.Application.Reference;

public static class CategoryQueries
{
    /// <summary>
    /// Categories that can be chosen on a complaint form: active, in an active group, listed group by group.
    /// Forms group categories in the order they arrive, so group order comes from here.
    /// </summary>
    public static IQueryable<ComplaintCategory> UsableOnForms(this IQueryable<ComplaintCategory> categories) =>
        categories
            .Where(c => c.IsActive && c.Group!.IsActive)
            .OrderBy(c => c.Group!.SortOrder).ThenBy(c => c.Group!.Name)
            .ThenBy(c => c.SortOrder).ThenBy(c => c.Name);
}
