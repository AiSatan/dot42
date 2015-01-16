﻿using System.IO;
using System.Linq;
using System.Xml.Linq;
using Dot42.ResourcesLib;

namespace Dot42.Ide.Project
{
    public static class ItemTypeDetector
    {
        /// <summary>
        /// Try to detect the type of item based on the given file.
        /// </summary>
        public static bool TryDetectItemType(string path, string frameworkFolder, out string itemType)
        {
            itemType = null;
            var ext = ConfigurationQualifiers.GetExtension(path) ?? string.Empty;

            switch (ext.ToLower())
            {
                case ".gif":
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".9.png":
                    itemType = Dot42Constants.ItemTypeDrawableResource;
                    return true;
                case ".xml":
                    return TryDetectXmlItemType(path, frameworkFolder, out itemType);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Try to detect the type of item based on the given file.
        /// </summary>
        private static bool TryDetectXmlItemType(string path, string frameworkFolder, out string itemType)
        {
            itemType = null;
            // Try loading the file.
            try
            {
                var doc = XDocument.Load(path);
                var root = doc.Root;
                if (root == null)
                    return false;

                switch (root.Name.LocalName)
                {
                    case "manifest":
                        itemType = Dot42Constants.ItemTypeManifestResource;
                        break;
                    case "ViewGroup":
                    case "View":
                    case "AbsoluteLayout":
                    case "FrameLayout":
                    case "GridLayout":
                    case "LinearLayout":
                    case "RelativeLayout":
                        itemType = Dot42Constants.ItemTypeLayoutResource;
                        break;
                    case "bitmap":
                    case "clip":
                    case "nine-patch":
                    case "inset":
                    case "layer-list":
                    case "level-list":
                    case "selector":
                    case "scale":
                    case "shape":
                    case "transition":
                        itemType = Dot42Constants.ItemTypeDrawableResource;
                        break;
                    case "menu":
                        itemType = Dot42Constants.ItemTypeMenuResource;
                        break;
                    case "resources":
                        itemType = Dot42Constants.ItemTypeValuesResource;
                        break;
                    case "set":
                        itemType = Dot42Constants.ItemTypeAnimationResource;
                        break;
                    default:
                        TryDetectLayoutXmlItemType(root, frameworkFolder, out itemType);
                        break;
                }
                return (itemType != null);
            }
            catch
            {
                // Ignore
                return false;
            }
        }

        /// <summary>
        /// Try to detect layout files.
        /// </summary>
        private static bool TryDetectLayoutXmlItemType(XElement root, string frameworkFolder, out string itemType)
        {
            var descriptorProviderSet = Descriptors.Descriptors.GetDescriptorProviderSet(frameworkFolder);
            var descriptors = descriptorProviderSet.LayoutDescriptors;
            if (descriptors.RootDescriptors.Any(x => x.Name == root.Name.LocalName))
            {
                // It's a layout descriptor
                itemType = Dot42Constants.ItemTypeLayoutResource;
                return true;
            }
            itemType = null;
            return false;
        }
    }
}