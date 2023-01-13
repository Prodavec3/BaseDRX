using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Addendum;

namespace Sungero.Docflow.Client
{
  partial class AddendumActions
  {
    public virtual void UnbindAddendum(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Запретить смену типа, если включен строгий доступ к документу.
      if (_obj.AccessRights.StrictMode != AccessRightsStrictMode.None)
      {
        Dialogs.ShowMessage(Addendums.Resources.DisableStrictModeToUnbindAddendum, MessageType.Error);
        return;
      }
      
      // Запретить смену типа, если документ зарегистрирован или зарезервирован.
      if (_obj.RegistrationState == OfficialDocument.RegistrationState.Registered &&
          _obj.DocumentKind.NumberingType != DocumentKind.NumberingType.Numerable ||
          _obj.RegistrationState == OfficialDocument.RegistrationState.Reserved)
      {
        Dialogs.ShowMessage(Addendums.Resources.NeedCancelRegistration, MessageType.Error);
        return;
      }
      
      // Запретить смену типа, если по документу есть активные задачи согласования по регламенту.
      if (Functions.OfficialDocument.Remote.HasApprovalTasksWithCurrentDocument(_obj))
      {
        Dialogs.ShowMessage(Addendums.Resources.NeedAbortApproval, MessageType.Error);
        return;
      }
      
      // Запретить смену типа, если документ или его тело заблокировано.
      var isCalledByDocument = CallContext.CalledDirectlyFrom(OfficialDocuments.Info);
      if (isCalledByDocument && Functions.Module.IsLockedByOther(_obj) ||
          !isCalledByDocument && Functions.Module.IsLocked(_obj) ||
          Functions.Module.VersionIsLocked(_obj.Versions.ToList()))
      {
        Dialogs.ShowMessage(Addendums.Resources.UnbindLockError, MessageType.Error);
        return;
      }
      
      _obj.Relations.RemoveFrom(Constants.Module.AddendumRelationName, _obj.LeadingDocument);
      _obj.Relations.Save();
      
      var convertedDocument = SimpleDocuments.As(_obj.ConvertTo(SimpleDocuments.Info));
      try
      {
        if (convertedDocument == null)
          throw AppliedCodeException.Create(Addendums.Resources.ConvertToSimpleDocumentError);
        
        convertedDocument.LifeCycleState = LifeCycleState.Obsolete;
        convertedDocument.LeadingDocument = null;
        
        // Добавляем параметр, чтобы показать хинт пользователю в сконвертированном документе и удалить связь.
        ((Sungero.Domain.Shared.IExtendedEntity)convertedDocument).Params.Add(Constants.Addendum.UnbindAddendumParamName, true);
        e.CloseFormAfterAction = true;
        convertedDocument.Show();
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat(Addendums.Resources.UnbindLogError, ex, ex.Message);
        Dialogs.ShowMessage(Addendums.Resources.UnbindError, MessageType.Error);
      }
    }

    public virtual bool CanUnbindAddendum(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted && !_obj.State.IsChanged && _obj.AccessRights.CanUpdate();
    }

  }

}