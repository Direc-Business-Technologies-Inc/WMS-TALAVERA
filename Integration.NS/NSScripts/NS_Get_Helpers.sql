SELECT
    id AS NetsuiteEmployeeInternalId,
    entityid as EmployeeCode,
	firstname as FirstName,
    lastname as LastName,
FROM employee
WHERE BUILTIN.DF(custentity_dbti_other_roles) LIKE '%Helper%'