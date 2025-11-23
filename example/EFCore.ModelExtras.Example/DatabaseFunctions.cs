using System.Diagnostics.CodeAnalysis;
using EFCore.ModelExtras.FunctionsAndTriggers;

namespace EFCore.ModelExtras.Example;

/// <summary>
/// Static declarations of all database functions used in this application.
/// </summary>
public static class DatabaseFunctions
{
    /// <summary>
    /// Trigger function that logs email changes to the audit table.
    /// </summary>
    public static readonly FunctionDeclaration LogUserEmailChange = new(
        "log_user_email_change",
        /*language=sql*/"""
        CREATE OR REPLACE FUNCTION log_user_email_change()
          RETURNS trigger
          LANGUAGE plpgsql
        AS $function$
        BEGIN
            -- Only log if email actually changed
            IF (TG_OP = 'UPDATE' AND OLD.email IS DISTINCT FROM NEW.email) THEN
                INSERT INTO email_audit_logs (user_id, old_email, new_email, changed_at)
                VALUES (NEW.id, OLD.email, NEW.email, NOW());
            END IF;

            RETURN NEW;
        END;
        $function$
        """
    );

    /// <summary>
    /// Trigger function that automatically sets updated_at timestamp.
    /// </summary>
    public static readonly FunctionDeclaration UpdateTimestamp = new(
        "update_timestamp",
        /*language=sql*/"""
        CREATE OR REPLACE FUNCTION update_timestamp()
          RETURNS trigger
          LANGUAGE plpgsql
        AS $function$
        BEGIN
            NEW.updated_at = NOW();
            RETURN NEW;
        END;
        $function$
        """
    );
}
