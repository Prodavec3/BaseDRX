using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore;

namespace Sungero.Exchange.Shared
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Проверить, относится ли документ к счетам-фактурам или УПД.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Признак того, является ли документ счетом-фактурой или УПД.</returns>
    /// <remarks>По возможности надо пользоваться сервисными признаками, для накладных из Диадока - врёт.</remarks>
    public static bool IsInvoiceFlowDocument(Docflow.IOfficialDocument document)
    {
      return FinancialArchive.UniversalTransferDocuments.Is(document) ||
        FinancialArchive.IncomingTaxInvoices.Is(document) ||
        FinancialArchive.OutgoingTaxInvoices.Is(document);
    }
    
    /// <summary>
    /// Проверка, есть ли у текущего пользователя сертификат сервиса обмена.
    /// </summary>
    /// <param name="businessUnitBox">Абонентский ящик нашей организации.</param>
    /// <returns>True, если есть, иначе False.</returns>
    public virtual bool HasCurrentUserExchangeServiceCertificate(IBusinessUnitBox businessUnitBox)
    {
      // Получить доступные сертификаты. 
      var availableCertificates = Functions.Module.Remote.GetCertificates(Users.Current).AsEnumerable();
      // Проверить наличие сертификатов ответственного, если сервис предоставляет такую возможность.
      if (businessUnitBox.HasExchangeServiceCertificates == true)
        availableCertificates = availableCertificates.Where(x => businessUnitBox.ExchangeServiceCertificates.Any(z => z.Certificate.Equals(x)));
      
      return availableCertificates.Any();
    }
  }
}