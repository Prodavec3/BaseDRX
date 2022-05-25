using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.DocumentGrantRightsQueueItem
{

  /// <summary>
  /// Прокси класс для элемента очереди.
  /// </summary>
  partial class ProxyQueueItem
  {
    public int Id { get; set; }
    
    public Guid Discriminator { get; set; }
    
    public int DocumentId_Docflow_Sungero { get; set; }
    
    public int AccessRights_Docflow_Sungero { get; set; }
    
    public string ChangedType_Docflow_Sungero { get; set; }
  }
  
}