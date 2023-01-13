using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.FinancialArchive.Waybill;

namespace Sungero.FinancialArchive.Shared
{
  partial class WaybillFunctions
  {
    /// <summary>
    /// Определить ответственного за документ.
    /// </summary>
    /// <returns>Ответственный за документ.</returns>
    public override Sungero.Company.IEmployee GetDocumentResponsibleEmployee()
    {
      if (_obj.ResponsibleEmployee != null)
        return _obj.ResponsibleEmployee;
      
      return base.GetDocumentResponsibleEmployee();
    }
    
    /// <summary>
    /// Получить список адресатов с электронной почтой для отправки вложением в письмо.
    /// </summary>
    /// <returns>Список адресатов.</returns>
    [Public]
    public override List<Sungero.Docflow.Structures.OfficialDocument.IEmailAddressee> GetEmailAddressees()
    {
      return Functions.Module.GetEmailAddressees(_obj);
    }

    [Public]
    public override bool IsVerificationModeSupported()
    {
      return true;
    }
  }
}