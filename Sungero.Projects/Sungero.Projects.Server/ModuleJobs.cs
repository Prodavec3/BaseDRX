using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Projects.Server
{
  public class ModuleJobs
  {

    /// <summary>
    /// Выдача прав на проекты и проектные папки.
    /// </summary>
    public virtual void GrantAccessRightsToProjectFolders()
    {
      var startDate = Calendar.Now;
      var lastStartDate = Docflow.PublicFunctions.Module.GetLastAgentRunDate(Constants.Module.LastProjectRightsUpdateDate);
      
      // Измененные проекты.
      Logger.DebugFormat("GrantAccessRightsToProjectAndProjectFolders: Start get changed projects.");
      var queue = new List<Sungero.Projects.Structures.ProjectRightsQueueItem.ProxyQueueItem>();
      
      var changedProjectIds = Projects.GetAll(d => d.Modified >= lastStartDate && d.Modified <= startDate).Select(d => d.Id);
      foreach (var projectId in changedProjectIds)
      {
        var project = Projects.GetAll(d => d.Id == projectId).FirstOrDefault();
        if (project == null)
          continue;
        
        queue.AddRange(Functions.Module.CreateProjectAccessRightsQueueItems(project));
      }
      
      var table = ProjectRightsQueueItems.Info.DBTableName;
      var ids = Sungero.Domain.IdentifierGenerator.GenerateIdentifiers(table, queue.Count).ToList();
      for (int i = 0; i < queue.Count; i++)
        queue[i].Id = ids[i];
      Docflow.PublicFunctions.Module.WriteStructuresToTable(table, queue);
      Logger.DebugFormat("GrantAccessRightsToProjectsAndProjectFolders: Added to queue {0} projects.", queue.Count);
      
      // Обновить дату запуска агента в базе.
      Docflow.PublicFunctions.Module.UpdateLastAgentRunDate(Constants.Module.LastProjectRightsUpdateDate, startDate);
      
      // Выдать права на проекты и папки проектов.
      var step = 5;
      var error = 0;
      var isEmpty = false;
      for (int i = 0; i < 10000; i = i + step)
      {
        // Если элементов больше нет - заканчиваем.
        if (isEmpty)
          break;
        
        var result = Transactions.Execute(
          () =>
          {
            Logger.DebugFormat("GrantAccessRightsToProjectsAndProjectFolders: Start process queue from {0}.", i);

            // Т.к. в конце транзакции элементы удаляются, в Take берем просто N новых элементов.
            var queueItemPart = ProjectRightsQueueItems.GetAll().Skip(error).Take(step).ToList();
            if (!queueItemPart.Any())
            {
              // Завершаем транзакцию, если больше нечего обрабатывать.
              isEmpty = true;
              return;
            }

            var accessRightsGranted = queueItemPart
              .Where(q => Functions.Module.GrantRightsToProjectAndFolder(q))
              .ToList();
            if (accessRightsGranted.Any())
              Docflow.PublicFunctions.Module.FastDeleteQueueItems(accessRightsGranted.Select(a => a.Id).ToList());
            error += queueItemPart.Count - accessRightsGranted.Count;
          });
        if (!result)
          error += step;
      }
      
    }
    
    /// <summary>
    /// Автоматическая выдача прав на проектные документы.
    /// </summary>
    public virtual void GrantAccessRightsToProjectDocuments()
    {
      var startDate = Calendar.Now;
      var lastStartDate = Docflow.PublicFunctions.Module.GetLastAgentRunDate(Constants.Module.LastProjectDocumentRightsUpdateDate);
      
      // Измененные документы.
      Logger.DebugFormat("GrantAccessRightsToProjectDocuments: Start get changed projects documents.");
      var queue = new List<Sungero.Projects.Structures.ProjectDocumentRightsQueueItem.ProxyQueueItem>();
      var changedDocumentIds = Docflow.OfficialDocuments.GetAll(d => d.Modified >= lastStartDate && d.Modified <= startDate && 
                                                                (d.Project != null || (Docflow.Addendums.Is(d) && d.LeadingDocument.Project != null)))
        .Select(d => d.Id);
      foreach (var documentId in changedDocumentIds)
      {
        var document = Docflow.OfficialDocuments.GetAll(d => d.Id == documentId).FirstOrDefault();
        if (document == null)
          continue;
        
        var project = Docflow.PublicFunctions.OfficialDocument.GetProject(document);        
        if (project == null)
          continue;
        queue.AddRange(Functions.Module.CreateAccessRightsProjectDocumentQueueItemWithAddendum(document.Id, project.Id));
      }
      
      // Измененные проекты.
      Logger.DebugFormat("GrantAccessRightsToProjectDocuments: Start get changed projects members documents.");
      var changedProjectsQueueItems = ProjectMemberRightsQueueItems.GetAll().ToList();
      foreach (var memberQueueItem in changedProjectsQueueItems)
      {
        var project = Projects.GetAll(p => p.Id == memberQueueItem.ProjectId).FirstOrDefault();
        if (project == null)
          continue;
        
        var allProjects = ModuleFunctions.GetProjectAndSubProjects(project).Cast<Docflow.IProjectBase>().ToList();
        
        var changedMembersDocuments = Docflow.OfficialDocuments.GetAll(d => allProjects.Contains(d.Project));

        foreach (var document in changedMembersDocuments)
          queue.AddRange(Functions.Module.CreateAccessRightsProjectDocumentQueueItemWithAddendum(document.Id, project.Id));
      }
      
      var table = ProjectDocumentRightsQueueItems.Info.DBTableName;
      var ids = Sungero.Domain.IdentifierGenerator.GenerateIdentifiers(table, queue.Count).ToList();
      for (int i = 0; i < queue.Count; i++)
        queue[i].Id = ids[i];
      Docflow.PublicFunctions.Module.WriteStructuresToTable(table, queue);
      Logger.DebugFormat("GrantAccessRightsToProjectDocuments: Added to queue {0} documents.", queue.Count);
      
      if (changedProjectsQueueItems.Any())
        Docflow.PublicFunctions.Module.FastDeleteQueueItems(changedProjectsQueueItems.Select(q => q.Id).ToList());
      
      // Обновить дату запуска агента в базе.
      Docflow.PublicFunctions.Module.UpdateLastAgentRunDate(Constants.Module.LastProjectDocumentRightsUpdateDate, startDate);
      
      // Выдать права на документы.
      var step = 5;
      var error = 0;
      var isEmpty = false;
      for (int i = 0; i < 10000; i = i + step)
      {
        // Если элементов больше нет - заканчиваем.
        if (isEmpty)
          break;
        
        var result = Transactions.Execute(
          () =>
          {
            Logger.DebugFormat("GrantAccessRightsToProjectDocuments: Start process queue from {0}.", i);

            // Т.к. в конце транзакции элементы удаляются, в Take берем просто N новых элементов.
            var queueItemPart = ProjectDocumentRightsQueueItems.GetAll().Skip(error).Take(step).ToList();
            if (!queueItemPart.Any())
            {
              // Завершаем транзакцию, если больше нечего обрабатывать.
              isEmpty = true;
              return;
            }

            var accessRightsGranted = queueItemPart
              .Where(q => Functions.Module.AddDocumentToFolder(q) && Functions.Module.GrantRightsToProjectDocuments(q))
              .ToList();
            if (accessRightsGranted.Any())
              Docflow.PublicFunctions.Module.FastDeleteQueueItems(accessRightsGranted.Select(a => a.Id).ToList());
            error += queueItemPart.Count - accessRightsGranted.Count;
          });
        if (!result)
          error += step;
      }
    }

  }
}