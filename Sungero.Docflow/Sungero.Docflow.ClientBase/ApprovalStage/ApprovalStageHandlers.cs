using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalStage;

namespace Sungero.Docflow
{
  partial class ApprovalStageClientHandlers
  {

    public virtual IEnumerable<Enumeration> RightTypeFiltering(IEnumerable<Enumeration> query)
    {
      if (_obj.StageType == StageType.Print || _obj.StageType == StageType.Execution ||
          _obj.StageType == StageType.Sending || _obj.StageType == StageType.CheckReturn)
        return query.Where(e => !e.Equals(RightType.FullAccess));
      
      if (_obj.StageType == StageType.Register)
        return query.Where(e => !e.Equals(RightType.Read));
      
      return query;
    }
    
    public virtual IEnumerable<Enumeration> ReworkTypeFiltering(IEnumerable<Enumeration> query)
    {
      if (_obj.StageType == StageType.Approvers || _obj.StageType == StageType.Manager ||
          _obj.StageType == StageType.Sign || _obj.StageType == StageType.SimpleAgr)
        return query.Where(e => !e.Equals(ReworkType.AfterComplete));
      
      return query;
    }
    
    public override void Closing(Sungero.Presentation.FormClosingEventArgs e)
    {
      if (_obj.State.IsInserted)
      {
        _obj.Name = string.Empty;
        _obj.Status = CoreEntities.DatabookEntry.Status.Closed;
      }
    }

    public virtual void StartDelayDaysValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0)
        e.AddError(ApprovalStages.Resources.IncorrectStartDelayDays);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      Functions.ApprovalStage.SetPropertiesVisibility(_obj);
      Functions.ApprovalStage.SetPropertiesAvailability(_obj);
      
      Functions.ApprovalStageBase.ShowEditWarning(_obj, e);

      Functions.ApprovalStage.SetRequiredProperties(_obj);
      
      if (CallContext.CalledFrom(ApprovalRuleBases.Info))
        _obj.State.Properties.StageType.IsEnabled = false;
      
      var сanRegister = true;
      if (!e.Params.TryGetValue(Constants.ApprovalStage.CanRegister, out сanRegister))
      {
        сanRegister = Functions.ApprovalStage.Remote.ClerkCanRegister(_obj);
        e.Params.Add(Constants.ApprovalStage.CanRegister, сanRegister);
      }
      
      // Проверка прав регистратора.
      if (!сanRegister)
        e.AddWarning(ApprovalStages.Resources.CantRegister);
    }
  }
}