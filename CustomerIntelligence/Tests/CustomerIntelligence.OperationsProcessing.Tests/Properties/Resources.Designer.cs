﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace NuClear.CustomerIntelligence.OperationsProcessing.Tests.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("NuClear.CustomerIntelligence.OperationsProcessing.Tests.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;?xml version=&quot;1.0&quot; encoding=&quot;utf-8&quot;?&gt;
        ///&lt;FirmForecast BranchCode=&quot;1&quot;&gt;
        ///  &lt;Firms&gt;
        ///    &lt;Firm Code=&quot;141274359267368&quot; ForecastClick=&quot;45&quot; ForecastAmount=&quot;670.00&quot;&gt;
        ///      &lt;Rubrics&gt;
        ///        &lt;Rubric Code=&quot;110365&quot; ForecastClick=&quot;23&quot; ForecastAmount=&quot;230.00&quot; /&gt;
        ///        &lt;Rubric Code=&quot;10792&quot; ForecastClick=&quot;22&quot; ForecastAmount=&quot;440.00&quot; /&gt;
        ///      &lt;/Rubrics&gt;
        ///    &lt;/Firm&gt;
        ///    &lt;Firm Code=&quot;141274359267381&quot; ForecastClick=&quot;55&quot; ForecastAmount=&quot;1000.00&quot;&gt;
        ///      &lt;Rubrics&gt;
        ///        &lt;Rubric Code=&quot;110365&quot; ForecastClick=&quot;10&quot; Foreca [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string flowStatistics_FirmForecast_xml {
            get {
                return ResourceManager.GetString("flowStatistics.FirmForecast.xml", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;FirmPopularity BranchCode=&quot;112&quot;&gt;
        ///  &lt;Firms&gt;
        ///    &lt;Firm Code=&quot;70000001019319210&quot; WeightAll=&quot;2&quot; WeightRecoveryAndGeo=&quot;1&quot;&gt;
        ///      &lt;Rubrics&gt;
        ///        &lt;Rubric Code=&quot;272&quot; ClickCount=&quot;1&quot; ImpressionCount=&quot;148&quot; /&gt;
        ///        &lt;Rubric Code=&quot;676&quot; ClickCount=&quot;0&quot; ImpressionCount=&quot;35&quot; /&gt;
        ///      &lt;/Rubrics&gt;
        ///    &lt;/Firm&gt;
        ///    &lt;Firm Code=&quot;70000001019319244&quot; WeightAll=&quot;3&quot; WeightRecoveryAndGeo=&quot;1&quot;&gt;
        ///      &lt;Rubrics&gt;
        ///        &lt;Rubric Code=&quot;295&quot; ClickCount=&quot;2&quot; ImpressionCount=&quot;52&quot; /&gt;
        ///        &lt;Rubric Code=&quot;459&quot; ClickCount=&quot;2&quot; Impre [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string flowStatistics_FirmPopularity_xml {
            get {
                return ResourceManager.GetString("flowStatistics.FirmPopularity.xml", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;RubricPopularity&gt;
        ///  &lt;Branch Code=&quot;112&quot; /&gt;
        ///  &lt;Rubrics&gt;
        ///    &lt;Rubric Priority=&quot;36363&quot; Code=&quot;116&quot; WeightAll=&quot;100002&quot; FirmCount=&quot;18&quot; AdvFirmCount=&quot;0&quot; AdvFirmShare=&quot;0.00&quot; /&gt;
        ///    &lt;Rubric Priority=&quot;307692&quot; Code=&quot;122&quot; WeightAll=&quot;100004&quot; FirmCount=&quot;8&quot; AdvFirmCount=&quot;0&quot; AdvFirmShare=&quot;0.00&quot; /&gt;
        ///  &lt;/Rubrics&gt;
        ///&lt;/RubricPopularity&gt;
        ///.
        /// </summary>
        internal static string flowStatistics_RubricPopularity_xml {
            get {
                return ResourceManager.GetString("flowStatistics.RubricPopularity.xml", resourceCulture);
            }
        }
    }
}