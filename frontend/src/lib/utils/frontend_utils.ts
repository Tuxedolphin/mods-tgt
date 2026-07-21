import { debounce } from "es-toolkit";
import { Err, Ok, Result } from "ts-results-es";
import { check_handle } from "./db_operations";
import type { RoomRole } from "$lib/types/db_raw_types";

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
