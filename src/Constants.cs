namespace Miniblog.Core
{
    using System.Diagnostics.CodeAnalysis;

    public static class Constants
    {
        public static readonly string AllCats = "AllCats";
        public static readonly string categories = "categories";
        public static readonly string Dash = "-";
        public static readonly string Description = "Description";
        public static readonly string Head = "Head";
        public static readonly string next = "next";
        public static readonly string page = "page";
        public static readonly string Preload = "Preload";
        public static readonly string prev = "prev";
        public static readonly string ReturnUrl = "ReturnUrl";
        public static readonly string Scripts = "Scripts";
        public static readonly string slug = "slug";
        public static readonly string Space = " ";
        public static readonly string Title = "Title";
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
}
