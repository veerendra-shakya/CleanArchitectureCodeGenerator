using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.CodeGenerator.Models
{
    public class PathMapping
    {
        /// <summary>
        /// The target path where the scaffolded file should be generated.
        /// </summary>
        public string TargetPath { get; set; }

        /// <summary>
        /// The relative path to the target location from the project's root.
        /// </summary>
        public string RelativeTargetPath { get; set; }

        /// <summary>
        /// The full path to the target location on the file system.
        /// </summary>
        public string FullTargetPath { get; set; }

        /// <summary>
        /// The template path from which the scaffolded file will be generated.
        /// </summary>
        public string TemplatePath { get; set; }

        /// <summary>
        /// The relative path to the template from the project's root.
        /// </summary>
        public string RelativeTemplatePath { get; set; }

        /// <summary>
        /// The full path to the template on the file system.
        /// </summary>
        public string FullTemplatePath { get; set; }
    }

}
