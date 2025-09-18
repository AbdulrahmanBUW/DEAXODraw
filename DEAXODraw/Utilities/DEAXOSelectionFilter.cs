using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace DEAXODraw.Utilities
{
    public class DEAXOSelectionFilter : ISelectionFilter
    {
        private readonly List<Type> _allowedTypes;
        private readonly List<ElementId> _allowedCategories;

        public DEAXOSelectionFilter(IEnumerable<object> typesOrCategories)
        {
            _allowedTypes = new List<Type>();
            _allowedCategories = new List<ElementId>();

            foreach (var item in typesOrCategories)
            {
                if (item is Type type)
                {
                    _allowedTypes.Add(type);
                }
                else if (item is BuiltInCategory category)
                {
                    _allowedCategories.Add(new ElementId(category));
                }
                else if (item is ElementId elementId)
                {
                    _allowedCategories.Add(elementId);
                }
            }
        }

        public bool AllowElement(Element elem)
        {
            try
            {
                // Exclude view-specific elements
                if (elem.ViewSpecific)
                    return false;

                // Check if element type is in allowed list
                if (_allowedTypes.Contains(elem.GetType()))
                    return true;

                // Check if element's category is in allowed list
                if (elem.Category != null && _allowedCategories.Contains(elem.Category.Id))
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in selection filter: {ex.Message}");
                return false;
            }
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }

    public static class CategoryHelper
    {
        public static Dictionary<string, object> GetSelectionOptions()
        {
            return new Dictionary<string, object>
            {
                {"Walls", BuiltInCategory.OST_Walls},
                {"Windows", BuiltInCategory.OST_Windows},
                {"Doors", BuiltInCategory.OST_Doors},
                {"Columns", new List<BuiltInCategory> { BuiltInCategory.OST_Columns, BuiltInCategory.OST_StructuralColumns }},
                {"Beams/Framing", BuiltInCategory.OST_StructuralFraming},
                {"Furniture", new List<BuiltInCategory> { BuiltInCategory.OST_Furniture, BuiltInCategory.OST_FurnitureSystems }},
                {"Plumbing Fixtures", new List<BuiltInCategory> { BuiltInCategory.OST_Furniture, BuiltInCategory.OST_PlumbingFixtures }},
                {"Generic Models", BuiltInCategory.OST_GenericModel},
                {"Casework", BuiltInCategory.OST_Casework},
                {"Curtain Walls", BuiltInCategory.OST_Walls},
                {"Lighting Fixtures", BuiltInCategory.OST_LightingFixtures},
                {"Mass", BuiltInCategory.OST_Mass},
                {"Parking", BuiltInCategory.OST_Parking},
                {"All Loadable Families", typeof(FamilyInstance)},
                {"Electrical Fixtures, Equipment, Circuits", new List<BuiltInCategory> {
                    BuiltInCategory.OST_ElectricalFixtures,
                    BuiltInCategory.OST_ElectricalEquipment,
                    BuiltInCategory.OST_ElectricalCircuit }}
            };
        }

        public static List<object> FlattenList(IEnumerable<object> list)
        {
            var result = new List<object>();

            foreach (var item in list)
            {
                if (item is List<BuiltInCategory> nestedList)
                {
                    result.AddRange(nestedList.Cast<object>());
                }
                else
                {
                    result.Add(item);
                }
            }

            return result;
        }
    }
}