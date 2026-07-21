CREATE TABLE IF NOT EXISTS public."Profiles" (
    "Id" uuid PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
    "Username" text,
    "Handle" text,
    "AvatarUpdatedAt" timestamp with time zone,
    "Colour" text,
    "DefaultTheme" text
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Profiles_Handle"
    ON public."Profiles" ("Handle");

CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS trigger AS $$
BEGIN
    INSERT INTO public."Profiles" ("Id", "Username", "Handle")
    VALUES (NEW.id, NULL, NULL);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

DROP TRIGGER IF EXISTS on_auth_user_created ON auth.users;
CREATE TRIGGER on_auth_user_created
    AFTER INSERT ON auth.users
    FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();

ALTER TABLE public."TimeTables"
DROP CONSTRAINT IF EXISTS "FK_TimeTables_Profiles";

ALTER TABLE public."TimeTables"
ADD CONSTRAINT "FK_TimeTables_Profiles"
FOREIGN KEY ("UserId") REFERENCES public."Profiles"("Id") ON DELETE CASCADE;

INSERT INTO public."Profiles" ("Id", "Username")
SELECT id, NULL FROM auth.users
WHERE id NOT IN (SELECT "Id" FROM public."Profiles");
