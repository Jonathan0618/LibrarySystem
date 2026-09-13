# Data Retention and Library Privacy Policy

Status: implementation baseline; university privacy/legal approval required before enabling automatic deletion.

## Current technical policy

- Active loans, reservations, fines, member accounts, and acquisition/inventory records are retained because they support current library obligations and collection accountability.
- Returned-loan history remains identifiable. The application does not silently anonymize it because the university has not approved an anonymization period or the effect on disputes, reporting, and legal holds.
- Audit logs default to seven years (2,555 days). Terminal notification records default to 90 days.
- Automated retention is disabled by default. It deletes only eligible audit logs and terminal notification records, in bounded batches, and creates an audit record for each run.
- No deletion may run while a litigation, investigation, public-records, audit, or other legal hold applies to the affected records.

## Approval before production enablement

The university privacy/legal owner must record: applicable jurisdiction and policy, approved periods, whether and when returned-loan history becomes anonymous, legal-hold owner and procedure, backup retention interaction, and approval date. Operations must take and verify a backup, preview eligible records, and then explicitly set `DataRetention__Enabled=true`.

## Staff responsibilities

Staff may access member and borrowing data only for library duties. They must not browse histories out of curiosity, disclose records to unauthorized people, export data to personal storage, share accounts, leave authenticated devices unattended, or place sensitive data in free-text notes. Exports must remain in university-approved encrypted storage and be deleted when their approved purpose ends. Suspected disclosure, incorrect access, lost exports, or compromised accounts must be reported through the university incident process immediately.

Requests to disclose, correct, preserve, or erase records must be referred to the university's designated privacy/legal owner; library staff must not improvise a response or bypass an active legal hold.
