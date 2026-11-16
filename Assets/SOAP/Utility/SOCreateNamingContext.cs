using System;

namespace SOAP
{
    /// <summary>
    /// Optional naming context an SO can implement so SOCreateDrawer can build
    /// a meaningful default asset name for [SOCreate] fields it owns, instead
    /// of falling back to the owner asset's own filename. QuestDefinitionSO
    /// and QuestObjectiveSO both expose their quest's QuestId here — notably,
    /// QuestObjectiveSO's own asset name doesn't contain it.
    /// </summary>
    public interface ISOCreateNamingContext
    {
        string NamingPrefix { get; }
    }

    /// <summary>
    /// Additional naming context for owners whose [SOCreate] fields should use
    /// the "{Prefix}_Objective{VarLabel}_{ShortName}" pattern (see
    /// QuestObjectiveSO) instead of the default "{Prefix}_{FieldName}_{TypeName}"
    /// pattern. Only meaningful combined with ISOCreateNamingContext.
    /// </summary>
    public interface ISOCreateVariableNamingContext
    {
        string VariableShortName { get; }
    }

    /// <summary>
    /// Short label substituted for a variable type's raw name when it's
    /// created inside an ISOCreateVariableNamingContext owner — e.g.
    /// BoolVariable -> "Bool", IntVariable -> "Counter". Falls back to the
    /// raw type name (SOCreateDrawer.GetVariableLabel) if a type has no label.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SOVariableLabelAttribute : Attribute
    {
        public string Label { get; }
        public SOVariableLabelAttribute(string label) => Label = label;
    }
}