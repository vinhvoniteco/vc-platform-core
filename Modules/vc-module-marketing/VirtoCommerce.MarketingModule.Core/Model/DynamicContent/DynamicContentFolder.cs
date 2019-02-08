using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.MarketingModule.Core.Model.DynamicContent
{
    public class DynamicContentFolder : AuditableEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public string Path
        {
            get
            {
                return ParentFolder == null ? Name : ParentFolder.Path + "\\" + Name;
            }
        }
        public string Outline
        {
            get
            {
                return ParentFolder == null ? Id : ParentFolder.Outline + ";" + Id;
            }
        }

        public string ParentFolderId { get; set; }
        public DynamicContentFolder ParentFolder { get; set; }
    }
}
