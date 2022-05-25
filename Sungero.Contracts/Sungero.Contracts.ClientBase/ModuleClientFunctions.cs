using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;

namespace Sungero.Contracts.Client
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Вызвать поиск договора по ссылке.
    /// </summary>
    /// <param name="uuid">Uuid договора в 1С.</param>
    /// <param name="number">Номер договора.</param>
    /// <param name="date">Дата договора.</param>
    /// <param name="butin">ИНН НОР.</param>
    /// <param name="butrrc">КПП НОР.</param>
    /// <param name="cuuid">Uuid контрагента в 1С.</param>
    /// <param name="ctin">ИНН контрагента.</param>
    /// <param name="ctrrc">КПП контрагента.</param>
    /// <param name="sysid">Код инстанса 1С.</param>
    [Hyperlink]
    public void FindContract(string uuid, string number, string date,
                             string butin, string butrrc,
                             string cuuid, string ctin, string ctrrc,
                             string sysid)
    {
      var result = Functions.Module.Remote.FindContract(uuid, number, date,
                                                        butin, butrrc,
                                                        cuuid, ctin, ctrrc,
                                                        sysid);
      
      if (!result.Any())
        Dialogs.ShowMessage("Договор не найден.");
      else if (result.Count == 1)
        result.First().Show();
      else
        result.Show();
    }
  }
}