using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.DocumentTemplate
{
  /// <summary>
  /// Шаблон документа.
  /// </summary>
  [Public]
  partial class DocumentTemplate
  {
    /// <summary>
    /// Тип документа, для которого предназначен шаблон.
    /// </summary>
    public Guid DocumentType { get; set; }

    /// <summary>
    /// Имя шаблона.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Расширение файла тела шаблона.
    /// </summary>
    public string Extension { get; set; }

    /// <summary>
    /// Список видов документов.
    /// </summary>
    public List<Guid> DocumentKinds { get; set; }

    /// <summary>
    /// Список параметров.
    /// </summary>
    public List<Sungero.Docflow.Structures.DocumentTemplate.IDocumentTemplateParameter> Mapping { get; set; }

    /// <summary>
    /// Тело шаблона.
    /// </summary>
    public byte[] Body { get; set; }

    /// <summary>
    /// Изображение шаблона для предпросмотра.
    /// </summary>
    public byte[] Preview { get; set; }
  }

  /// <summary>
  /// Параметр шаблона документа.
  /// </summary>
  [Public]
  partial class DocumentTemplateParameter
  {
    /// <summary>
    /// Порядковый номер.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Имя параметра.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Путь к свойству.
    /// </summary>
    public string DataSource { get; set; }
  }
}