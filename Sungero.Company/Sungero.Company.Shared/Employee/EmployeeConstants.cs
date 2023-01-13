namespace Sungero.Company.Constants
{
  public static class Employee
  {
    /// <summary>
    /// Настройки полей индекса с данными о сотрудниках.
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
              ""FirstName"": {""type"": ""text"", ""analyzer"": ""name_analyzer""},
              ""LastName"": {""type"": ""text"", ""analyzer"": ""name_analyzer""},
              ""Patronymic"": {""type"": ""text"", ""analyzer"": ""name_analyzer""},
              ""InitialFirstName"": {""type"": ""keyword""},
              ""InitialPatronymic"": {""type"": ""keyword""},
              ""BusinessUnitId"": {""type"": ""keyword""},
              ""Updated"": {""type"": ""date"", ""format"": ""dd.MM.yyyy HH:mm:ss""},
              ""Status"": {""type"": ""keyword""}
            }
          }
        }";
    
    /// <summary>
    /// Шаблон для создания индекса с данными о сотрудниках.
    /// </summary>
    public const string ElasticsearchIndexTemplate = "{{ \"Id\": \"{0}\", \"FullName\": \"{1}\", \"LastName\": \"{2}\", " +
      "\"FirstName\": \"{3}\", \"Patronymic\": \"{4}\", \"InitialFirstName\": \"{5}\", \"InitialPatronymic\": \"{6}\", " +
      "\"BusinessUnitId\": \"{7}\", \"Updated\": \"{8}\", \"Status\": \"{9}\" }}";
    
    /// <summary>
    /// Минимальная оценка при нечетком поиске сотрудников.
    /// </summary>
    public const double ElasticsearchMinScore = 2;

  }
}