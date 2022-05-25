using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Projects.Client
{
  public class ModuleFunctions
  {

    /// <summary>
    /// Создать документ по проекту.
    /// </summary>
    public virtual void CreateDocument()
    {
      ProjectDocuments.CreateDocumentWithCreationDialog(ProjectDocuments.Info,
                                                        Docflow.SimpleDocuments.Info,
                                                        Docflow.Addendums.Info,
                                                        Docflow.MinutesBases.Info);
    }

    /// <summary>
    /// Создать проект.
    /// </summary>
    public virtual void CreateProject()
    {
      Functions.Project.Remote.CreateProject().Show();
    }
    
    /// <summary>
    /// Отобразить однократно нотифайку о выдаче прав на проект, папки проекта, проектные документы.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    /// <param name="message">Текст уведомления.</param>
    [Public]
    public virtual void ShowProjectRightsNotifyOnce(Sungero.Domain.Shared.BaseEventArgs e, string message)
    {
      bool showProjectDocumentRightsNotify;
      if (!e.Params.TryGetValue(Constants.Project.ShowProjectRightsNotify, out showProjectDocumentRightsNotify))
      {
        Dialogs.NotifyMessage(message);
        e.Params.AddOrUpdate(Constants.Project.ShowProjectRightsNotify, false);
      }
    }
  }
}