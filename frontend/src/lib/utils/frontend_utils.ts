import { debounce } from "es-toolkit";
import { Err, Ok, Result } from "ts-results-es";
import { check_handle } from "./db_operations";
import type {
  RoomRole,
  RoomVisibility,
  ThemePreference,
} from "$lib/types/db_raw_types";

export const daisy_ui_themes = [
  "light",
  "dark",
  "cupcake",
  "bumblebee",
  "emerald",
  "corporate",
  "synthwave",
  "retro",
  "cyberpunk",
  "valentine",
  "halloween",
  "garden",
  "forest",
  "aqua",
  "lofi",
  "pastel",
  "fantasy",
  "wireframe",
  "black",
  "luxury",
  "dracula",
  "cmyk",
  "autumn",
  "business",
  "acid",
  "lemonade",
  "night",
  "coffee",
  "winter",
  "dim",
  "nord",
  "sunset",
  "caramellatte",
  "abyss",
  "silk",
];

export function sleep(ms: number) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

export function json_tryparse<T>(text: string): Result<T, string> {
  try {
    return Ok(JSON.parse(text));
  } catch {
    return Err(text);
  }
}

export function format_theme_preference_to_string(theme: ThemePreference) {
  switch (theme) {
    case "dark":
      return "Dark Theme";
    case "light":
      return "Light Theme";
    case "system":
      return "Follow System";
  }

  return "Invalid";
}

export function get_room_visibility_description(role: RoomVisibility): string {
  switch (role) {
    case "publicEdit":
      return "Allow anyone with a ModsTgt account to add their timetable";
    case "publicView":
      return "Allow anyone with a ModsTgt account to view this timetable";
    case "restricted":
      return "Only allow those on the list above to access this timetable";
  }

  return "Invalid";
}

export function format_room_visibility_to_string(role: RoomVisibility): string {
  switch (role) {
    case "publicEdit":
      return "Editable by anyone";
    case "publicView":
      return "Viewable by anyone";
    case "restricted":
      return "Restricted";
  }

  return "Invalid";
}

export function format_room_role_to_string(role: RoomRole): string {
  switch (role) {
    case "editor":
      return "Editor";
    case "owner":
      return "Owner";
    case "viewer":
      return "Viewer";
  }

  return "Invalid";
}

export const query_available_handle = debounce(
  async (
    handle: string,
    token_information: string,
    on_fail: (fail_text: string) => void,
    on_success: (success_text: string) => void,
  ) => {
    const result = await check_handle(handle, token_information);

    if (result.isOk()) {
      if (!result.value.available) {
        switch (result.value.reason) {
          case "invalidFormat":
            on_fail("Handle is in an invalid format, please try again");
            return;
          case "reserved":
            on_fail("Handle has already been reserved. Please try another");
            return;
          case "taken":
            on_fail("Handle is taken, please try another.");
            return;
          case "tooLong":
            on_fail("Handle is too long, please try another.");
            return;
          case "tooShort":
            on_fail("Handle is too short, must be at least 4 characters.");
            return;
          default:
            break;
        }
        return;
      }

      on_success("Handle is available!");
    }

    if (result.isErr()) {
      on_fail(result.error);
    }
  },
  500,
);
