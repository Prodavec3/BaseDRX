using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Commons.Server
{
  public class ModuleAsyncHandlers
  {
    
    /// <summary>
    /// Удалить сущность из индекса.
    /// </summary>
    /// <param name="args">Параметры вызова асинхронного обработчика.</param>
    public virtual void RemoveEntityFromIndex(Sungero.Commons.Server.AsyncHandlerInvokeArgs.RemoveEntityFromIndexInvokeArgs args)
    {
      // Отмена АО, в случае передачи неверных параметров.
      var invalidArgumentsPassed = false;
      if (string.IsNullOrWhiteSpace(args.IndexName))
      {
        Logger.Debug("Commons. RemoveEntityFromIndex. Invalid arguments passed: \"IndexName\" must be not empty. Operation will be cancel.");
        invalidArgumentsPassed = true;
      }
      if (args.EntityId <= 0)
      {
        Logger.Debug("Commons. RemoveEntityFromIndex. Invalid arguments passed: \"EntityId\" must be greater than 0. Operation will be cancel.");
        invalidArgumentsPassed = true;
      }
      if (invalidArgumentsPassed)
        return;
      
      Logger.DebugFormat("Commons. RemoveEntityFromIndex. Start remove entity with id {0} from index {1}. Retry iteration: {2}.",
                         args.EntityId, args.IndexName, args.RetryIteration);
      
      var result = false;
      var errorText = string.Empty;
      try
      {
        PublicFunctions.Module.ElasticsearchRemoveEntity(args.IndexName, args.EntityId);
        result = true;
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Commons. RemoveEntityFromIndex. Error while remove entity with id {0} from index {1}: {2}",
                           args.EntityId, args.IndexName, ex.Message);
      }
      
      if (!result)
      {
        if (args.RetryIteration < Constants.Module.IndexingAsyncsRetryCount)
        {
          args.Retry = true;
          Logger.DebugFormat("Commons. RemoveEntityFromIndex. Entity with id {0} not removed from index {1}, retry iteration {2}.",
                             args.EntityId, args.IndexName, args.RetryIteration);
        }
        else
          Logger.ErrorFormat("Commons. RemoveEntityFromIndex. Cant remove entity with id {0} from index {1}. Retry attempts are over, retry iteration: {2}.",
                             args.EntityId, args.IndexName, args.RetryIteration);
      }
      else
        Logger.DebugFormat("Commons. RemoveEntityFromIndex. Success remove entity with id {0} from index {1}.", args.EntityId, args.IndexName);
    }
    
    /// <summary>
    /// Индексировать сущность.
    /// </summary>
    /// <param name="args">Параметры вызова асинхронного обработчика.</param>
    public virtual void IndexEntity(Sungero.Commons.Server.AsyncHandlerInvokeArgs.IndexEntityInvokeArgs args)
    {
      // Отмена АО, в случае передачи неверных параметров.
      var invalidArgumentsPassed = false;
      if (string.IsNullOrWhiteSpace(args.IndexName))
      {
        Logger.Debug("Commons. IndexEntity. Invalid arguments passed: \"IndexName\" must be not empty. Operation will be cancel.");
        invalidArgumentsPassed = true;
      }
      if (string.IsNullOrWhiteSpace(args.Json))
      {
        Logger.Debug("Commons. IndexEntity. Invalid arguments passed: \"Json\" must be not empty. Operation will be cancel.");
        invalidArgumentsPassed = true;
      }
      if (args.EntityId <= 0)
      {
        Logger.Debug("Commons. IndexEntity. Invalid arguments passed: \"EntityId\" must be greater than 0. Operation will be cancel.");
        invalidArgumentsPassed = true;
      }
      if (invalidArgumentsPassed)
        return;
      
      Logger.DebugFormat("Commons. IndexEntity. Start indexing entity with id {0} to index {1}. Retry iteration: {2}.",
                         args.EntityId, args.IndexName, args.RetryIteration);
      
      var result = false;
      var errorText = string.Empty;
      try
      {
        PublicFunctions.Module.ElasticsearchIndexEntity(args.IndexName, args.Json, args.EntityId, args.AsyncCreated, args.AllowCreateRecord);
        result = true;
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Commons. IndexEntity. Error while indexing entity with id {0} to index {1}: {2}",
                           args.EntityId, args.IndexName, ex.Message);
      }
      
      if (!result)
      {
        if (args.RetryIteration < Constants.Module.IndexingAsyncsRetryCount)
        {
          args.Retry = true;
          Logger.DebugFormat("Commons. IndexEntity. Entity with id {0} not indexed to index {1}, retry iteration {2}.",
                             args.EntityId, args.IndexName, args.RetryIteration);
        }
        else
          Logger.ErrorFormat("Commons. IndexEntity. Cant index entity with id {0} to index {1}. Retry attempts are over, retry iteration: {2}.",
                             args.EntityId, args.IndexName, args.RetryIteration);
      }
      else
        Logger.DebugFormat("Commons. IndexEntity. Success index entity with id {0} to index {1}.", args.EntityId, args.IndexName);
    }
    
  }
}