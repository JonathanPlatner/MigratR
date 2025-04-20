-- Migration: 20250419184951_sample.sql
-- UP
-- Write your forward migration SQL statements here
create table test(
    id int identity(1,1),
    name nvarchar(24)
);
--//@ ```MIGRATION SEPARATOR: DO NOT DELETE THIS LINE```

-- Write your rollback migration SQL statements here
-- DOWN
drop table test;