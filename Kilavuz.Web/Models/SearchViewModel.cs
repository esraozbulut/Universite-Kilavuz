using System.Collections.Generic;

namespace Kilavuz.Web.Models;

public class SearchViewModel
{
    public string Query { get; set; } = string.Empty;
    public List<SearchResultItem> Results { get; set; } = new();
}

public class SearchResultItem
{
    public string ResultType { get; set; } = string.Empty; // Application, Category, Page
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Snippet { get; set; }
    public string? ParentName { get; set; }

    // Hiyerarşik URL için
    public int? AppId { get; set; }
    public int? CategoryId { get; set; }

    public string GetUrl() => ResultType switch
    {
        "Application" => $"/kilavuz/{Id}",
        "Category"    => $"/kilavuz/{AppId}/{Id}",
        "Page"        => $"/kilavuz/{AppId}/{CategoryId}/{Id}",
        _             => "/"
    };

    public string GetIcon() => ResultType switch
    {
        "Application" => "fa-cubes",
        "Category"    => "fa-folder-o",
        "Page"        => "fa-file-text-o",
        _             => "fa-search"
    };

    public string GetTypeName() => ResultType switch
    {
        "Application" => "Uygulama",
        "Category"    => "Kategori",
        "Page"        => "Sayfa",
        _             => ""
    };
}
