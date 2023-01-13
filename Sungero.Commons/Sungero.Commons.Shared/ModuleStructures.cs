using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Commons.Structures.Module
{
  #region Интеллектуальная обработка
  
  /// <summary>
  /// Факт.
  /// </summary>
  [Public]
  partial class ArioFact
  {
    // ИД факта в Арио.
    public int Id { get; set; }
    
    // Название факта.
    public string Name { get; set; }
    
    // Список полей.
    public List<Sungero.Commons.Structures.Module.IArioFactField> Fields { get; set; }
  }
  
  /// <summary>
  /// Поле факта.
  /// </summary>
  [Public]
  partial class ArioFactField
  {
    // ИД поля в Арио.
    public int Id { get; set; }
    
    // Название поля.
    public string Name { get; set; }
    
    // Значение поля.
    public string Value { get; set; }
    
    // Вероятность.
    public double Probability { get; set; }
  }
  
  /// <summary>
  /// Печать.
  /// </summary>
  [Public]
  partial class ArioStamp
  {
    // Вероятность.
    public double Probability { get; set; }
    
    // Позиция в документе.
    public string Position { get; set; }
    
    // Угол поворота.
    public double Angle { get; set; }
  }
  
  /// <summary>
  /// Подпись.
  /// </summary>
  [Public]
  partial class ArioSignature
  {
    // Вероятность.
    public double Probability { get; set; }
    
    // Позиция в документе.
    public string Position { get; set; }
    
    // Угол поворота.
    public double Angle { get; set; }
  }

  /// <summary>
  /// Поиск по значению поля в индексе Elasticsearch.
  /// </summary>
  [Public]
  partial class ArioFieldElasticsearchData
  {
    // Поле Ario.
    public Sungero.Commons.Structures.Module.IArioFactField ArioField { get; set; }
    
    // Имя искомой сущности.
    public string EntityName { get; set; }

    // Имя поля в индексе.
    public string ElasticFieldName { get; set; }
    
    // Тип поиска.
    public string SearchType { get; set; }

    // Искомое значение. Если не указано, используется значение из поля Ario.
    public string SearchValue { get; set; }
    
    // Признак необходимости поиска закрытых записей. 
    public bool IsClosedEntitySearch { get; set; }

    // Признак, что поиск по полю производится только для уточнения результатов ранее найденных сущностей.
    public bool IsRefineSearchOnly { get; set; }
    
    // Условие для выборки ранее найденных сущностей (json-строка API Elasticsearch).
    public string RefineSearchFilter { get; set; }
    
    // Значение оценки для ограничения результатов поиска (возвращаются записи с оценкой не ниже лимита).
    public double ScoreMinLimit { get; set; }
    
    // Процент для расчета лимита от максимально возможной оценки.
    public double ScoreLimitPercent { get; set; }

    // Число найденных записей с оценкой не ниже лимита.
    public int EntityCount { get; set; }
    
    // ИД найденной сущности, заполняется, если найдена единственная запись.
    public int EntityId { get; set; }
  }
  
  /// <summary>
  /// Поиск по указанным полям факта в индексе Elasticsearch.
  /// </summary>
  [Public]
  partial class ArioFactElasticsearchData
  {
    // Факт Ario.
    public Sungero.Commons.Structures.Module.IArioFact Fact { get; set; }

    // Имя искомой сущности.
    public string EntityName { get; set; }
    
    // Описания поиска для каждого поля.
    public List<Sungero.Commons.Structures.Module.IArioFieldElasticsearchData> Queries { get; set; }

    // ИД найденной сущности, заполняется, если найдена единственная запись.
    public int EntityId { get; set; }

    // Список полей Ario, по которым найдена/уточнена сущность.
    public List<Sungero.Commons.Structures.Module.IArioFactField> FoundedFields { get; set; }
    
    // Cредневзвешенная вероятность по найденным полям.
    public double AggregateFieldsProbability { get; set; }
  }
  
  #endregion
}