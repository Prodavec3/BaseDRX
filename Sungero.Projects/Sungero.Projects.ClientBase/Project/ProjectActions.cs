using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Projects.Project;

namespace Sungero.Projects.Client
{
  partial class ProjectActions
  {
    public virtual void ShowDocuments(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Project.Remote.GetProjectDocuments(_obj).Show(e.Action.LocalizedName);
    }

    public virtual bool CanShowDocuments(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }

    public virtual void ReopenProject(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      _obj.Stage = Stage.Execution;
      _obj.State.Properties.ActualStartDate.IsRequired = false;
      _obj.State.Properties.ActualFinishDate.IsRequired = false;
      _obj.Save();
    }

    public virtual bool CanReopenProject(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Stage == Stage.Completed && _obj.AccessRights.CanUpdate();
    }

    public virtual void CloseProject(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (_obj.ActualStartDate == null || _obj.ActualFinishDate == null)
      {
        _obj.State.Properties.ActualStartDate.IsRequired = true;
        _obj.State.Properties.ActualFinishDate.IsRequired = true;
        e.Validate();
        return;
      }
      
      var dialogMessage = Projects.Resources.CloseProjectDialogMessage;
      var dialogDescription = Projects.Resources.CloseProjectDialogDescription;
      var dialog = Dialogs.CreateTaskDialog(dialogMessage, dialogDescription, MessageType.Question);
      dialog.Buttons.AddYesNo();
      dialog.Buttons.Default = DialogButtons.Yes;
      var result = dialog.Show();
      
      if (result == DialogButtons.Yes)
      {
        _obj.Stage = Stage.Completed;
        _obj.Save();
      }
    }

    public virtual bool CanCloseProject(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Stage != Stage.Completed && _obj.AccessRights.CanUpdate();
    }

    public virtual void CreateProjectDocument(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var document = Functions.Project.Remote.CreateProjectDocument(_obj);
      
      document.Show();
    }

    public virtual bool CanCreateProjectDocument(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Folder != null && _obj.Stage != Stage.Completed;
    }

    public virtual void ShowFolder(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      _obj.Folder.Items.Show();
    }

    public virtual bool CanShowFolder(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Folder != null;
    }
  }
}