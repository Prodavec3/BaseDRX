using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.ExchangeCore.Structures.BusinessUnitBox
{
  /// <summary>
  /// Обработанный документ пришедший из системы обмена.
  /// </summary>
  partial class ExchangeProcessedDocument
  {
    public Docflow.IOfficialDocument Document { get; set; }
    
    public bool? HasDocumentReadPermissions { get; set; }
    
    public int? DocumentId { get; set; }
    
    public Exchange.IExchangeDocumentInfo DocumentInfo { get; set; }
  }
  
  /// <summary>
  /// Пакет совместно обрабатываемых документов и сообщений из системы обмена.
  /// </summary>
  partial class ExchangeDocumentsPackage
  {
    public List<Sungero.ExchangeCore.Structures.BusinessUnitBox.ExchangeProcessedDocument> Documents { get; set; }
    
    public List<ExchangeCore.IMessageQueueItem> Messages { get; set; }
  }
}