using System;

namespace OpenCliToMcp
{
    /// <summary>
    /// Marks a class to have MCP tool methods generated from the specified OpenCLI specification file.
    /// The class must be partial and non-static.
    /// </summary>
    /// <example>
    /// <code>
    /// [OpenCliTool("weather.opencli.json")]
    /// public partial class WeatherToolMcp
    /// {
    ///     private readonly ICliExecutor cliExecutor;
    ///     
    ///     public WeatherToolMcp(ICliExecutor cliExecutor)
    ///     {
    ///         this.cliExecutor = cliExecutor;
    ///     }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class OpenCliToolAttribute : Attribute
    {
        /// <summary>
        /// Gets the path to the OpenCLI specification file.
        /// The path can be absolute or relative to the source file containing this attribute.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenCliToolAttribute"/> class.
        /// </summary>
        /// <param name="filePath">
        /// The path to the OpenCLI specification file. 
        /// This should be an AdditionalFile in the project.
        /// The path can be absolute or relative to the source file.
        /// </param>
        public OpenCliToolAttribute(string filePath)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        }
    }
}