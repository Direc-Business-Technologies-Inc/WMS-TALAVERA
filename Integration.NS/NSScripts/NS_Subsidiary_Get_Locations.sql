SELECT
    l.id AS NetsuiteLocationInternalId,
    l.name AS LocationName,
    lsm.subsidiary,
    BUILTIN.DF(lsm.subsidiary)
FROM location l
JOIN locationSubsidiaryMap lsm
    ON l.id = lsm.location
WHERE lsm.subsidiary = @subsidiaryid