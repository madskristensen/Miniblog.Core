namespace Miniblog.Core;

public static class Constants
{
    public static readonly string AllCats = "AllCats";
    public static readonly string AllTags = "AllTags";
    public static readonly string Categories = "categories";
    public static readonly string Dash = "-";
    public static readonly string Description = "Description";
    public static readonly string Head = "Head";
    public static readonly string Next = "next";
    public static readonly string Page = "page";
    public static readonly string PostsPerPage = "PostsPerPage";
    public static readonly string Preload = "Preload";
    public static readonly string Prev = "prev";
    public static readonly string ReturnUrl = "ReturnUrl";
    public static readonly string Scripts = "Scripts";
    public static readonly string Slug = "slug";
    public static readonly string Space = " ";
    public static readonly string Tags = "tags";
    public static readonly string Title = "Title";
    public static readonly string TotalPages = "TotalPages";
    public static readonly string TotalPostCount = "TotalPostCount";
    public static readonly string ViewOption = "ViewOption";

    [SuppressMessage(
        "Design",
        "CA1034:Nested types should not be visible",
        Justification = "Constant classes are nested for easy intellisense.")]
    public static class Config
    {
        public static class Blog
        {
            public static readonly string Name = "blog:name";
        }

        public static class User
        {
            public static readonly string Password = "user:password";
            public static readonly string Salt = "user:salt";
            public static readonly string UserName = "user:username";
        }
    }
}
