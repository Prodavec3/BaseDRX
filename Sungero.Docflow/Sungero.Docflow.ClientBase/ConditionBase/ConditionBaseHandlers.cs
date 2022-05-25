using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ConditionBase;

namespace Sungero.Docflow
{
  partial class ConditionBaseClientHandlers
  {   

    public virtual void AmountValueInput(Sungero.Presentation.DoubleValueInputEventArgs e) 
    {
      if (e.NewValue < 0)
        e.AddError(ConditionBases.Resources.NegativeTotalAmount);
    }
    
    public virtual IEnumerable<Enumeration> ConditionTypeFiltering(IEnumerable<Enumeration> query) 
    {      
      if (!_obj.DocumentKinds.Any())
        return query;
      
      var possibleTypes = Docflow.PublicFunctions.ConditionBase.GetPossibleConditionTypes(_obj, Functions.ConditionBase.GetSupportedConditions(_obj));
      return query.Where(x => possibleTypes.Any(t => Equals(t, x)));
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e) 
    {
      var hasRules = false;
      
      if (!(_obj.State.IsInserted || _obj.State.IsCopied) && _obj.AccessRights.CanUpdate())
      {
        hasRules = Functions.ConditionBase.Remote.HasRules(_obj);
        
        if (hasRules)
        {
          e.AddInformation(ConditionBases.Resources.ConditionHasRules);
          _obj.State.Properties.ConditionType.IsEnabled = false;
        }
      }
      
      Functions.ConditionBase.ChangePropertiesAccess(_obj);
      
      var possibleTypes = Docflow.PublicFunctions.ConditionBase.GetPossibleConditionTypes(_obj, Functions.ConditionBase.GetSupportedConditions(_obj));
      
      if (_obj.State.IsInserted && possibleTypes.Count() == 1)
        _obj.ConditionType = possibleTypes.FirstOrDefault();
    }
  }
}