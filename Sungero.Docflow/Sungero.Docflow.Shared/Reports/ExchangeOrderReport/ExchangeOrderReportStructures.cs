using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.ExchangeOrderReport
{
  /// <summary>
  /// Структура для добавления данных в таблицу отчёта.
  /// </summary>
  partial class ExchangeOrderInfo
  {
    public string MessageType { get; set; }
    
    public string DocumentName { get; set; }
    
    public string SendedFrom { get; set; }
    
    public string Date { get; set; }
    
    public string ReportSessionId { get; set; }
  }
  
  /// <summary>
  /// Данные для построения отчета.
  /// </summary>
  partial class ExchangeOrderFullData
  {
    public List<Sungero.Docflow.Structures.ExchangeOrderReport.ExchangeOrderInfo> ExchangeOrderInfo { get; set; }
    
    public bool IsComplete { get; set; }
    
    public bool IsReceiptNotifications { get; set; }
    
    public bool IsSignOrAnnulment { get; set; }
    
    public bool IsReceipt { get; set; }
  }

}