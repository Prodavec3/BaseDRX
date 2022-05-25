using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.QueueItemBase;

namespace Sungero.ExchangeCore.Server
{
  partial class QueueItemBaseFunctions
  {
    /// <summary>
    /// Реакция элемента очереди при ошибке.
    /// </summary>
    /// <param name="errorMessage">Текст ошибки.</param>
    [Public]
    public void QueueItemOnError(string errorMessage)
    {
      _obj.Retries += 1;
      _obj.ProcessingStatus = ExchangeCore.QueueItemBase.ProcessingStatus.Error;
      
      var noteLength = _obj.Info.Properties.Note.Length;
      if (errorMessage.Length > noteLength)
        errorMessage = errorMessage.Substring(0, noteLength);
      _obj.Note = errorMessage;
      
      _obj.Save();
    }
  }
}