update sungero_company_assistant
set  isassistant = true
where isassistant is null;

update sungero_company_assistant
set  preparesasgcmp = false
where preparesasgcmp is null;