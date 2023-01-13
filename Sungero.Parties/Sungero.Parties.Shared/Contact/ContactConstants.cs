using System;

namespace Sungero.Parties.Constants
{
  public static class Contact
  {
    /// <summary>
    /// Настройки полей индекса с данными о контактах.
    /// </summary>
    public const string ElasticsearchIndexConfig = @"{
        ""settings"": {
          ""index"": {
              ""analysis"": {
                ""char_filter"": {
                  ""e_char_filter"": {
                    ""type"": ""mapping"",
                    ""mappings"": [ ""Ё => Е"", ""ё => е"" ]
                    }
                  },
                  ""analyzer"": {
                      ""name_analyzer"": {
                          ""type"": ""custom"",
                          ""tokenizer"": ""standard"",
                          ""filter"": [
                              ""lowercase"",
                              ""russian_morphology""
                          ],
                          ""char_filter"": [""e_char_filter""]
                      }
                  }
              }
          }
      },
      ""mappings"": {
          ""properties"": {
            ""Id"": {""type"": ""keyword""},
            ""FullName"": {""type"": ""text"", ""analyzer"": ""name_analyzer""},
            ""CompanyId"": {""type"": ""keyword""},
            ""Updated"": {""type"": ""date"", ""format"": ""dd.MM.yyyy HH:mm:ss""},
            ""Status"": {""type"": ""keyword""}
          }
        }
      }";

    /// <summary>
    /// Шаблон для создания индекса с данными о контактах.
    /// </summary>
    public const string ElasticsearchIndexTemplate = "{{ \"Id\": \"{0}\", \"CompanyId\": \"{1}\", \"FullName\": \"{2}\", \"Updated\": \"{3}\", \"Status\": \"{4}\" }}";
    
    /// <summary>
    /// Минимальная оценка при нечетком поиске контактов.
    /// </summary>
    public const double ElasticsearchMinScore = 2;
  }
}