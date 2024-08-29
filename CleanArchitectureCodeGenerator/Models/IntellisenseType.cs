using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.CodeGenerator.Models
{
    public class IntellisenseType
    {
        /// <summary>
        /// This is the name of this type as it appears in the source code
        /// </summary>
        [Display(Name = "Code Name", Description = "The name of the type as it appears in the source code.")]
        public string CodeName { get; set; }

        /// <summary>
        /// Indicates whether this type is array. If this property is true, then all other properties
        /// describe not the type itself, but rather the type of the array's elements.
        /// </summary>
        [Display(Name = "Is Array", Description = "Indicates whether this type is an array.")]
        public bool IsArray { get; set; }

        [Display(Name = "Is Dictionary", Description = "Indicates whether this type is a dictionary.")]
        public bool IsDictionary { get; set; }

        [Display(Name = "Is Optional", Description = "Indicates whether this type is optional.")]
        public bool IsOptional
        {
            get { return CodeName.EndsWith("?"); }
        }

        /// <summary>
        /// If this type is itself part of a source code file that has a .d.ts definitions file attached,
        /// this property will contain the full (namespace-qualified) client-side name of that type.
        /// Otherwise, this property is null.
        /// </summary>
        [Display(Name = "Client Side Reference Name", Description = "The full namespace-qualified client-side name of the type.")]
        public string ClientSideReferenceName { get; set; }

        /// <summary>
        /// This is TypeScript-formed shape of the type (i.e. inline type definition). It is used for the case where
        /// the type is not primitive, but does not have its own named client-side definition.
        /// </summary>
        [Display(Name = "Shape", Description = "TypeScript-formed shape of the type.")]
        public IEnumerable<IntellisenseProperty> Shape { get; set; }

        [Display(Name = "Is Known Type", Description = "Indicates whether this type is a known TypeScript type.")]
        public bool IsKnownType
        {
            get { return TypeScriptName != "any"; }
        }

        [Display(Name = "TypeScript Name", Description = "The TypeScript name of the type.")]
        public string TypeScriptName
        {
            get
            {
                if (IsDictionary) return GetKVPTypes();
                return GetTargetName(CodeName, false);
            }
        }

        private string GetTargetName(string codeName, bool js)
        {
            var t = codeName.ToLowerInvariant().TrimEnd('?');
            switch (t)
            {
                case "int16":
                case "int32":
                case "int64":
                case "short":
                case "int":
                case "long":
                case "float":
                case "double":
                case "decimal":
                case "biginteger":
                    return js ? "Number" : "number";

                case "datetime":
                case "datetimeoffset":
                case "system.datetime":
                case "system.datetimeoffset":
                    return "Date";

                case "string":
                    return js ? "String" : "string";

                case "bool":
                case "boolean":
                    return js ? "Boolean" : "boolean";
            }
            return js ? "Object" : GetComplexTypeScriptName();
        }

        private string GetComplexTypeScriptName()
        {
            return ClientSideReferenceName ?? "any";
        }

        private string GetKVPTypes()
        {
            var type = CodeName.ToLowerInvariant().TrimEnd('?');
            var types = type.Split('<', '>')[1].Split(',');
            string keyType = GetTargetName(types[0].Trim(), false);

            if (keyType != "string" && keyType != "number")
            { // only string or number are allowed for keys
                keyType = "string";
            }

            string valueType = GetTargetName(types[1].Trim(), false);

            return string.Format(CultureInfo.CurrentCulture, "{{ [index: {0}]: {1} }}", keyType, valueType);
        }
    }
}