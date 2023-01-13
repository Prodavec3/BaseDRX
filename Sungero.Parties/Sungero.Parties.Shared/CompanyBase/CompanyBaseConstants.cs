namespace Sungero.Parties.Constants
{
  public static class CompanyBase
  {
    public const string FindedContactsInServiceParamName = "FindedContactsInService";
    
    public const string ActiveCounterpartyStateInService = "Действующее";
    
    /// <summary>
    /// Настройки полей индекса с данными о контрагентах.
    /// </summary>
    public const string ElasticsearchIndexConfig = @"{{
      ""settings"": {{
        ""index"" : {{
          ""similarity"" : {{
            ""default"" : {{
              ""type"" : ""BM25"",
              ""b"": 0,
              ""k1"": 0
            }}
          }},
          ""analysis"": {{
            ""filter"": {{
              ""my_synonyms"": {{
                 ""type"": ""synonym_graph"",
                 ""synonyms"": [{0}],
                 ""expand"": true,
                 ""updateable"": true
              }}
            }},
            ""char_filter"": {{
              ""e_char_filter"": {{
                ""type"": ""mapping"",
                ""mappings"": [ ""Ё => Е"", ""ё => е"" ]
              }}
            }},
            ""analyzer"": {{
              ""name_index_analyzer"": {{
                ""tokenizer"": ""standard"",
                ""filter"": [ ""lowercase"" ],
                ""char_filter"": [""e_char_filter""]
              }},
              ""name_query_analyzer"": {{
                ""tokenizer"": ""standard"",
                ""filter"": [
                  ""lowercase"",
                  ""my_synonyms""
                ],
              ""char_filter"": [""e_char_filter""]
              }},
              ""email_analyzer"":{{
                ""tokenizer"" : ""uax_url_email"",
                ""filter"" : [""lowercase""]
              }}
            }}
          }}
        }}
      }},
      ""mappings"": {{
        ""properties"": {{
          ""Id"": {{""type"": ""keyword""}},
          ""Name"": {{
            ""type"": ""text"",
            ""analyzer"": ""name_index_analyzer"",
            ""search_analyzer"": ""name_query_analyzer""
          }},
          ""ShortName"": {{
            ""type"": ""text"",
            ""analyzer"": ""name_index_analyzer"",
            ""search_analyzer"": ""name_query_analyzer""
          }},
          ""HeadCompany"": {{""type"": ""text""}},
          ""TIN"": {{""type"": ""keyword""}},
          ""TRRC"": {{""type"": ""keyword""}},
          ""PSRN"": {{""type"": ""keyword""}},
          ""Address"": {{""type"": ""text""}},
          ""Homepage"": {{""type"": ""text""}},
          ""Email"": {{""type"": ""text"", ""analyzer"": ""email_analyzer""}},
          ""Phones"": {{""type"": ""text""}},
          ""Updated"": {{""type"": ""date"", ""format"": ""dd.MM.yyyy HH:mm:ss""}},
          ""Status"": {{""type"": ""keyword""}}
          }}
        }}
      }}";
    
    /// <summary>
    /// Шаблон для создания индекса с данными о контрагентах.
    /// </summary>
    public const string ElasticsearchIndexTemplate = "{{ \"Id\": \"{0}\", \"Name\": \"{1}\", \"ShortName\": \"{2}\", " +
      "\"HeadCompany\": \"{3}\", \"TIN\": \"{4}\", \"TRRC\": \"{5}\", \"PSRN\": \"{6}\", \"Homepage\": \"{7}\", " +
      "\"Email\": \"{8}\", \"Phones\": \"{9}\", \"Address\": \"{10}\", \"Updated\": \"{11}\", \"Status\": \"{12}\" }}";
    
    /// <summary>
    /// Минимальная оценка при нечетком поиске контрагентов.
    /// </summary>
    [Sungero.Core.Public]
    public const double ElasticsearchMinScore = 5;
  }
}