using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.AcquaintanceTask;

namespace Sungero.RecordManagement.Shared
{
  partial class AcquaintanceTaskFunctions
  {

    /// <summary>
    /// Валидация старта задачи на ознакомление.
    /// </summary>
    /// <param name="e">Аргументы действия.</param>
    /// <returns>True, если валидация прошла успешно, и False, если были ошибки.</returns>
    public virtual bool ValidateAcquaintanceTaskStart(Sungero.Core.IValidationArgs e)
    {
      var errorMessages = Sungero.RecordManagement.Functions.AcquaintanceTask.Remote.GetStartValidationMessage(_obj);
      if (errorMessages.Any())
      {
        foreach (var error in errorMessages)
        {
          if (error.IsShowNotAutomatedEmployeesMessage)
            e.AddError(error.Message, _obj.Info.Actions.ShowNotAutomatedEmployees);
          else if (error.IsCantSendTaskByNonEmployeeMessage)
            e.AddError(_obj.Info.Properties.Author, error.Message);
          else
            e.AddError(error.Message);
        }
        return false;
      }
      
      return true;
    }
    
    /// <summary>
    /// Сохранить номер версии и хеш документа в задаче.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="isMainDocument">Признак главного документа.</param>
    public void StoreAcquaintanceVersion(IElectronicDocument document, bool isMainDocument)
    {
      var lastVersion = document.LastVersion;
      var mainDocumentVersion = _obj.AcquaintanceVersions.AddNew();
      mainDocumentVersion.IsMainDocument = isMainDocument;
      mainDocumentVersion.DocumentId = document.Id;
      if (lastVersion != null)
      {
        mainDocumentVersion.Number = lastVersion.Number;
        mainDocumentVersion.Hash = lastVersion.Body.Hash;
      }
      else
      {
        mainDocumentVersion.Number = 0;
        mainDocumentVersion.Hash = null;
      }
    }
  }
}