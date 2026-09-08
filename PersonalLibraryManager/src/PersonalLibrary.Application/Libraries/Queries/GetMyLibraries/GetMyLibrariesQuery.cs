using PersonalLibrary.Application.Common.Pagination;

namespace PersonalLibrary.Application.Libraries.Queries.GetMyLibraries;

public sealed record GetMyLibrariesQuery(PageRequest Page);
