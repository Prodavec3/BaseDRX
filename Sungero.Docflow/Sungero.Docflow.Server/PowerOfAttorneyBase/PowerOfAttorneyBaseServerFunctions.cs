using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.PowerOfAttorneyBase;

namespace Sungero.Docflow.Server
{
  partial class PowerOfAttorneyBaseFunctions
  {
    
    /// <summary>
    /// Получить список записей справочника Право подписи, созданных на основе доверенности.
    /// </summary>
    /// <returns>Список прав подписи.</returns>
    [Remote]
    public IQueryable<ISignatureSetting> GetSignatureSettingsByPOA()
    {
      return Docflow.SignatureSettings.GetAll(s => Equals(s.Document, _obj));
    }
    
    /// <summary>
    /// Получить список действующих записей справочника Право подписи, созданных на основе доверенности.
    /// </summary>
    /// <returns>Список прав подписи.</returns>
    [Remote]
    public IQueryable<ISignatureSetting> GetActiveSignatureSettingsByPOA()
    {
      var signSetting = Functions.PowerOfAttorneyBase.GetSignatureSettingsByPOA(_obj);
      return signSetting.Where(s => Equals(s.Status, Docflow.SignatureSetting.Status.Active));
    }
  }
}