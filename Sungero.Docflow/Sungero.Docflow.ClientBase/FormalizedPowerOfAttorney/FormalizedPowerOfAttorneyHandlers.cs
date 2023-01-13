using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FormalizedPowerOfAttorney;

namespace Sungero.Docflow
{
  partial class FormalizedPowerOfAttorneyClientHandlers
  {

    public override void Closing(Sungero.Presentation.FormClosingEventArgs e)
    {
      base.Closing(e);
      
      e.Params.Remove(Constants.FormalizedPowerOfAttorney.HideImportParamName);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      // Занесение доверенности не завершено. Не дизейблить обязательные поля, чтобы можно было завершить занесение.
      if (_obj.RegistrationState == Docflow.OfficialDocument.RegistrationState.Registered &&
          _obj.LifeCycleState == Docflow.OfficialDocument.LifeCycleState.Draft &&
          (_obj.IssuedTo == null || _obj.BusinessUnit == null || _obj.Department == null))
      {
        e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.RepeatRegister, true);
        e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.NeedValidateRegisterFormat, true);
      }
      
      // Запретить повторный импорт, если уже занесена подписанная версия.
      if (!e.Params.Contains(Constants.FormalizedPowerOfAttorney.HideImportParamName) &&
          Signatures.Get(_obj.LastVersion).Any(x => x.SignatureType == SignatureType.Approval))
      {
        e.Params.AddOrUpdate(Constants.FormalizedPowerOfAttorney.HideImportParamName, true);
      }
      
      base.Refresh(e);
    }

    public virtual void UnifiedRegistrationNumberValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (!string.IsNullOrEmpty(e.NewValue))
        e.NewValue = e.NewValue.Trim();
      
      Guid guid;
      if (!Guid.TryParse(e.NewValue, out guid))
        e.AddError(Sungero.Docflow.FormalizedPowerOfAttorneys.Resources.ErrorValidateUnifiedRegistrationNumber);
    }

  }
}