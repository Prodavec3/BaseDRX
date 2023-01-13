using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Projects.Project;

namespace Sungero.Projects.Server
{
  partial class ProjectFunctions
  {
    /// <summary>
    /// Создать проект.
    /// </summary>
    /// <returns>Проект.</returns>
    [Remote]
    public static IProject CreateProject()
    {
      return Projects.Create();
    }
    
    /// <summary>
    /// Вложить документ в папку проекта.
    /// </summary>
    /// <param name="document">Вкладываемый документ.</param>
    /// <remarks>Проект определяет, где должны лежать ссылки на его документы.</remarks>
    [Public]
    public virtual void AddDocumentToFolder(Content.IElectronicDocument document)
    {
      var folders = new List<IFolder>();
      var official = Docflow.OfficialDocuments.As(document);
      if (official != null)
      {
        var classifierFolders = _obj.Classifier.Where(l => Equals(l.DocumentKind, official.DocumentKind)).Select(l => l.Folder).Distinct().ToList();
        if (classifierFolders.Any())
          folders.AddRange(classifierFolders);
        else
          folders.Add(_obj.Folder);
      }
      else
        folders.Add(_obj.Folder);
      
      foreach (var folder in folders.Where(l => !l.Items.Contains(document)))
      {
        folder.Items.Add(document);
        Logger.DebugFormat("GrantAccessRightsToProjectDocuments: document (id = {0}) added to folder (id = {1})", document.Id, folder.Id);
      }
    }
    
    /// <summary>
    /// Создать папки для классификатора.
    /// </summary>
    public virtual void UpdateClassifier()
    {
      var groups = _obj.Classifier.GroupBy(l => l.FolderName).ToList();
      foreach (var nameGroup in groups)
      {
        var changedName = nameGroup.Where(l => l.State.Properties.FolderName.OriginalValue != l.FolderName).ToList();
        foreach (var changed in changedName)
        {
          // Переименовывать папку, только если однозначно можно сказать, что папки с таким наименованием нет и не было в классификаторе.
          if (_obj.Classifier.Any(f => (f.FolderName == changed.State.Properties.FolderName.OriginalValue ||
                                        f.FolderName == changed.FolderName ||
                                        f.State.Properties.FolderName.OriginalValue == changed.State.Properties.FolderName.OriginalValue) && f.Id != changed.Id))
            changed.Folder = null;
        }
        
        var folder = nameGroup.Select(g => g.Folder).Where(f => f != null).Distinct().SingleOrDefault();
        if (folder == null)
          folder = Folders.Create();

        folder.Name = nameGroup.Key;
        _obj.Folder.Items.Add(folder);
        
        foreach (var line in nameGroup.Where(l => l.Folder == null))
          line.Folder = folder;
        
        // Заполнение папки содержимым при изменении классификатора.
        if (_obj.State.Properties.Classifier.IsChanged)
        {
          var kinds = nameGroup.Select(g => g.DocumentKind).ToList();
          var documents = Docflow.OfficialDocuments.GetAll().Where(d => Equals(d.Project, _obj) && kinds.Contains(d.DocumentKind));
          foreach (var doc in documents)
            folder.Items.Add(doc);
        }
      }
    }
    
    /// <summary>
    /// Получить документы по проекту.
    /// </summary>
    /// <returns>Документы по проекту.</returns>
    [Remote]
    public IQueryable<IOfficialDocument> GetProjectDocuments()
    {
      var query = OfficialDocuments.GetAll().Where(d => Equals(d.Project, _obj));
      
      return query;
    }
    
    /// <summary>
    /// Создать документ по проекту.
    /// </summary>
    /// <returns>Документ.</returns>
    [Remote]
    public IOfficialDocument CreateProjectDocument()
    {
      var document = ProjectDocuments.Create();
      document.Project = _obj;
      return document;
    }
    
    /// <summary>
    /// Получить проекты по рукодителю или администратору проекта.
    /// </summary>
    /// <param name="recipient">Роль/сотрудник.</param>
    /// <returns>Проекты.</returns>
    public static List<IProject> GetProjectsManagerOrAdministrator(CoreEntities.IRecipient recipient)
    {
      var projects = new List<IProject>();
      projects = Projects.GetAll(p => Equals(p.Manager, recipient) || Equals(p.Administrator, recipient)).ToList();
      projects.SelectMany(p => Functions.Module.GetProjectAndSubProjects(p));
      return projects;
    }
  }
}