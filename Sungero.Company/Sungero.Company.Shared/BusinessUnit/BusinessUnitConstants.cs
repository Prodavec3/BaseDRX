namespace Sungero.Company.Constants
{
  public static class BusinessUnit
  {
    /// <summary>
    /// Настройки полей индекса с данными о НОР.
    /// </summary>
    public const string ElasticsearchIndexConfig = @"{{
      ""settings"":{{
        ""index"":{{
           ""similarity"":{{
              ""default"":{{
                 ""type"":""BM25"",
                 ""b"":0,
                 ""k1"":0
              }}
           }},
           ""analysis"":{{
              ""filter"": {{
                 ""my_synonyms"": {{
                    ""type"": ""synonym_graph"",
                    ""synonyms"": [{0}],
                    ""expand"": true,
                    ""updateable"": true
                 }}
              }},
              ""char_filter"":{{
                 ""e_char_filter"":{{
                    ""type"":""mapping"",
                    ""mappings"":[
                       ""Ё => Е"",
                       ""ё => е""
                    ]
                 }}
              }},
              ""analyzer"":{{
                 ""text_index_analyzer"":{{
                    ""tokenizer"":""standard"",
                    ""filter"":[
                       ""lowercase""
                    ],
                    ""char_filter"":[
                       ""e_char_filter""
                    ]
                 }},
                 ""text_query_analyzer"":{{
                    ""tokenizer"":""standard"",
                    ""filter"":[
                       ""lowercase"",
                       ""my_synonyms""
                    ],
                    ""char_filter"":[
                       ""e_char_filter""
                    ]
                 }}
              }}
           }}
        }}
      }},
      ""mappings"":{{
        ""properties"":{{
           ""Id"":{{
              ""type"":""keyword""
           }},
           ""Name"":{{
              ""type"":""text"",
              ""analyzer"":""text_index_analyzer"",
              ""search_analyzer"":""text_query_analyzer""
           }},
           ""ShortName"":{{
              ""type"":""text"",
              ""analyzer"":""text_index_analyzer"",
              ""search_analyzer"":""text_query_analyzer""
           }},
           ""TIN"":{{
              ""type"":""keyword""
           }},
           ""TRRC"":{{
              ""type"":""keyword""
           }},
           ""PSRN"":{{
              ""type"":""keyword""
           }},
           ""Updated"":{{
              ""type"":""date"",
              ""format"":""dd.MM.yyyy HH:mm:ss""
           }},
           ""Status"":{{
              ""type"":""keyword""
           }}
        }}
      }}
    }}";

    /// <summary>
    /// Шаблон для создания индекса с данными о НОР.
    /// </summary>
    public const string ElasticsearchIndexTemplate = "{{ \"Id\": \"{0}\", \"Name\": \"{1}\", \"ShortName\": \"{2}\", \"TIN\": \"{3}\", " +
      "\"TRRC\": \"{4}\", \"PSRN\": \"{5}\", \"Updated\": \"{6}\", \"Status\": \"{7}\" }}";

    /// <summary>
    /// Минимальная оценка при нечетком поиске НОР.
    /// </summary>
    [Sungero.Core.Public]
    public const double ElasticsearchMinScore = 5;
  }
}