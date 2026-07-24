import ky, { HTTPError } from "ky";
import { Err, Ok, type Result } from "ts-results-es";
import { goto } from "$app/navigation";
import { resolve } from "$app/paths";
import { PUBLIC_DB_LINK } from "$env/static/public";
import {
  currentUserInformation,
  token_information,
} from "$lib/shared/shared.svelte";
import type {
  AuthResponse,
  AuthSucessResponse,
  ErrorInformation,
  ErrorResponse,
  HandleAvailabilityResponse,
  Profile,
  ProfileValidationErrorResponse,
  TimetableInfos,
  TimetablePostTemplate,
  TimetableResponse,
  TimetableSummaryResponse,
} from "../types/db_raw_types";
import { json_tryparse } from "./frontend_utils";

const apiCalls = ky.create({
  baseUrl: PUBLIC_DB_LINK,
});

type CustomOptions = {
  authorised?: boolean;
  unauthorizedCheck?: boolean;
  auth_token?: string;
};

function create_ky_instance(custom_options: CustomOptions) {
  return apiCalls.extend({
    hooks: {
      beforeRequest: [
        ({ request }) => {
          if (custom_options.authorised) {
            request.headers.set(
              "Authorization",
              `Bearer ${custom_options.auth_token}`,
            );
          }
        },
      ],
      afterResponse: [
        async ({ response }) => {
          if (custom_options.unauthorizedCheck && response.status === 401) {
            token_information.reset();
            currentUserInformation.reset();
            const message = "Login expired, please login in again";
            goto(resolve(`/login#error_description=${message}`));
          }
        },
      ],
    },
  });
}

export async function register_db(
  email: string,
  password: string,
): Promise<Result<AuthSucessResponse, string>> {
  try {
    const register = create_ky_instance({
      authorised: false,
      unauthorizedCheck: false,
    }).extend({
      json: {
        email: email,
        password: password,
      },
    });
    const register_json = await register
      .post("auth/register")
      .json<AuthSucessResponse>();

    return new Ok(register_json);
  } catch (error) {
    try {
      if (error instanceof HTTPError) {
        const errorResponse = error.data as ErrorResponse;
        const errorMessage = json_tryparse<ErrorInformation>(
          errorResponse.title,
        );

        if (errorMessage.isOk()) {
          return Err(errorMessage.value.msg);
        }

        return Err(errorMessage.error);
      }
    } catch {
      return Err("Error Registering. Please try again");
    }

    return Err("Error Registering. Please try again.");
  }
}

export async function login_to_db(
  username: string,
  password: string,
): Promise<Result<AuthResponse, string>> {
  try {
    const login_db = create_ky_instance({
      authorised: false,
      unauthorizedCheck: false,
    }).extend({
      json: {
        email: username,
        password: password,
      },
    });
    const login_json = await login_db.post("auth/login").json<AuthResponse>();
    return new Ok(login_json);
  } catch (error) {
    try {
      if (error instanceof HTTPError) {
        const errorResponse = error.data as ErrorResponse;
        const errorMessage = json_tryparse<ErrorInformation>(
          errorResponse.title,
        );

        if (errorMessage.isOk()) {
          return Err(errorMessage.value.msg);
        }

        return Err(errorMessage.error);
      }
    } catch {
      return new Err("Wrong username or password");
    }

    return new Err("Wrong username or password");
  }
}

export async function put_user_info(
  access_token: string,
  username: string,
): Promise<Result<string, string>> {
  try {
    const put_user_db = create_ky_instance({
      authorised: true,
      unauthorizedCheck: true,
      auth_token: access_token,
    }).extend({
      json: {
        username: username,
      },
    });
    await put_user_db.put("profile/me");
    return Ok(username);
  } catch (error) {
    try {
      if (error instanceof HTTPError) {
        const errorResponse = error.data as ErrorResponse;
        const errorMessage = json_tryparse<ErrorInformation>(
          errorResponse.title,
        );

        if (errorMessage.isOk()) {
          return Err(errorMessage.value.msg);
        }

        return Err(errorMessage.error);
      }
    } catch {
      return new Err("Wrong username or password");
    }

    return new Err("Wrong username or password");
  }
}

export async function delete_user_profile_photo(
  access_token: string,
): Promise<Result<string, string>> {
  try {
    const delete_profile_picture_db = create_ky_instance({
      auth_token: access_token,
      authorised: true,
      unauthorizedCheck: true,
    });

    await delete_profile_picture_db.delete("profile/avatar");

    return Ok("");
  } catch (error) {
    try {
      if (error instanceof HTTPError) {
        const errorResponse = error.data as ErrorResponse;
        const errorMessage = json_tryparse<ErrorInformation>(
          errorResponse.title,
        );

        if (errorMessage.isOk()) {
          return Err(errorMessage.value.msg);
        }

        return Err(errorMessage.error);
      }
    } catch {
      return new Err("Error deleting image");
    }

    return new Err("Error deleting image");
  }
}

export async function check_handle(
  handle: string,
  access_token: string,
): Promise<Result<HandleAvailabilityResponse, string>> {
  try {
    const check_handle_db = create_ky_instance({
      auth_token: access_token,
      authorised: true,
      unauthorizedCheck: true,
    });

    if (handle === "") {
      return Err("Handle not provided");
    }

    const result = await check_handle_db
      .get("profile/check-handle", {
        searchParams: {
          handle: handle,
        },
      })
      .json<HandleAvailabilityResponse>();

    return Ok(result);
  } catch (error) {
    try {
      if (error instanceof HTTPError) {
        const errorResponse = error.data as ErrorResponse;
        const errorMessage = json_tryparse<ErrorInformation>(
          errorResponse.title,
        );

        if (errorMessage.isOk()) {
          return Err(errorMessage.value.msg);
        }

        return Err(errorMessage.error);
      }
    } catch {
      return new Err("Error changing image. Please try again");
    }

    return new Err("Error changing image. Please try again.");
  }
}

export async function update_user_preferences(
  user_update_data: Profile,
  access_token: string,
): Promise<Result<string, string>> {
  try {
    const update_profile_db = create_ky_instance({
      auth_token: access_token,
      authorised: true,
      unauthorizedCheck: true,
    }).extend({
      json: user_update_data,
    });

    await update_profile_db.put("profile/me/customisation");
    return Ok("");
  } catch (error) {
    console.log(error);
    try {
      if (error instanceof HTTPError) {
        const errorResponse = error.data as ErrorResponse;
        const errorMessage = json_tryparse<ErrorInformation>(
          errorResponse.title,
        );

        if (errorMessage.isOk()) {
          return Err(errorMessage.value.msg);
        }

        const parse_validation_error =
          error.data as ProfileValidationErrorResponse;

        if (
          parse_validation_error.errors.Handle &&
          parse_validation_error.errors.Handle.length != 0
        ) {
          return Err(parse_validation_error.errors.Handle[0]);
        }

        if (
          parse_validation_error.errors.Username &&
          parse_validation_error.errors.Username.length != 0
        ) {
          return Err(parse_validation_error.errors.Username[0]);
        }

        return Err("Error updating profile information. Please try again.");
      }
    } catch {
      return Err("Error updating profile information. Please try again.");
    }

    return Err("Error updating profile information. Please try again.");
  }
}

export async function update_user_profile(
  user_update_data: Profile,
  access_token: string,
): Promise<Result<string, string>> {
  try {
    const update_profile_db = create_ky_instance({
      auth_token: access_token,
      authorised: true,
      unauthorizedCheck: true,
    }).extend({
      json: user_update_data,
    });

    const result = await update_profile_db.put("profile/me");

    return Ok("");
  } catch (error) {
    console.log(error);
    try {
      if (error instanceof HTTPError) {
        console.log(error);
        const errorResponse = error.data as ErrorResponse;
        const errorMessage = json_tryparse<ErrorInformation>(
          errorResponse.title,
        );

        if (errorMessage.isOk()) {
          return Err(errorMessage.value.msg);
        }

        const parse_validation_error =
          error.data as ProfileValidationErrorResponse;

        if (
          parse_validation_error.errors.Handle &&
          parse_validation_error.errors.Handle.length != 0
        ) {
          return Err(parse_validation_error.errors.Handle[0]);
        }

        if (
          parse_validation_error.errors.Username &&
          parse_validation_error.errors.Username.length != 0
        ) {
          return Err(parse_validation_error.errors.Username[0]);
        }

        return Err("Error updating profile information. Please try again.");
      }
    } catch {
      return Err("Error updating profile information. Please try again.");
    }

    return Err("Error updating profile information. Please try again.");
  }
}

export async function update_user_password(
  old_password: string,
  new_password: string,
  access_token: string,
): Promise<Result<string, string>> {
  try {
    const update_pw_db = create_ky_instance({
      auth_token: access_token,
      authorised: true,
      unauthorizedCheck: true,
    }).extend({
      json: {
        oldPassword: old_password,
        newPassword: new_password,
      },
    });

    await update_pw_db.post("auth/update-password");

    return Ok("");
  } catch (error) {
    console.log(error);
    try {
      if (error instanceof HTTPError) {
        const errorResponse = error.data as ErrorResponse;
        const errorMessage = json_tryparse<ErrorInformation>(
          errorResponse.title,
        );

        if (errorMessage.isOk()) {
          return Err(errorMessage.value.msg);
        }

        const parse_validation_error =
          error.data as ProfileValidationErrorResponse;

        if (
          parse_validation_error.errors.Handle &&
          parse_validation_error.errors.Handle.length != 0
        ) {
          return Err(parse_validation_error.errors.Handle[0]);
        }

        if (
          parse_validation_error.errors.Username &&
          parse_validation_error.errors.Username.length != 0
        ) {
          return Err(parse_validation_error.errors.Username[0]);
        }

        return Err("Error update password. Please try again.");
      }
    } catch {
      return Err("Error update password. Please try again.");
    }

    return Err("Error update password. Please try again.");
  }
}

export async function update_user_profile_photo(
  image_data: Blob,
  access_token: string,
): Promise<Result<string, string>> {
  const data = new FormData();
  data.append("file", image_data);
  try {
    const update_profile_db = create_ky_instance({
      auth_token: access_token,
      authorised: true,
      unauthorizedCheck: true,
    }).extend({
      body: data,
    });

    await update_profile_db.put("profile/avatar");

    return Ok("");
  } catch (error) {
    try {
      if (error instanceof HTTPError) {
        const errorResponse = error.data as ErrorResponse;
        const errorMessage = json_tryparse<ErrorInformation>(
          errorResponse.title,
        );

        if (errorMessage.isOk()) {
          return Err(errorMessage.value.msg);
        }

        return Err(errorMessage.error);
      }
    } catch {
      return new Err("Error changing image. Please try again");
    }

    return new Err("Error changing image. Please try again.");
  }
}

export async function delete_user(
  access_token: string,
): Promise<Result<string, string>> {
  try {
    const get_user_info_db = create_ky_instance({
      authorised: true,
      unauthorizedCheck: true,
      auth_token: access_token,
    });

    await get_user_info_db.delete("profile/me");
    return Ok("Deleted");
  } catch (error) {
    try {
      if (error instanceof HTTPError) {
        const errorResponse = error.data as ErrorResponse;
        const errorMessage = json_tryparse<ErrorInformation>(
          errorResponse.title,
        );

        if (errorMessage.isOk()) {
          return Err(errorMessage.value.msg);
        }

        return Err(errorMessage.error);
      }
    } catch {
      return new Err("Error deleting user");
    }

    return new Err("Error deleting user");
  }
}

export async function get_user_info(
  access_token: string,
  force_cache_refresh: boolean = false,
): Promise<Result<Profile, string>> {
  try {
    const get_user_info_db = create_ky_instance({
      authorised: true,
      unauthorizedCheck: true,
      auth_token: access_token,
    });

    if (force_cache_refresh) {
      get_user_info_db.extend({
        cache: "reload",
      });
    }
    const timetables = await get_user_info_db.get("profile/me").json<Profile>();

    currentUserInformation.set(timetables);
    return Ok(timetables);
  } catch (error) {
    try {
      if (error instanceof HTTPError) {
        const errorResponse = error.data as ErrorResponse;
        const errorMessage = json_tryparse<ErrorInformation>(
          errorResponse.title,
        );

        if (errorMessage.isOk()) {
          return Err(errorMessage.value.msg);
        }

        return Err(errorMessage.error);
      }
    } catch {
      return new Err("Wrong username or password");
    }

    return new Err("Wrong username or password");
  }
}

export async function get_timetables(
  access_token: string,
): Promise<Result<TimetableInfos, string>> {
  try {
    const get_timetables_db = create_ky_instance({
      authorised: true,
      unauthorizedCheck: true,
      auth_token: access_token,
    });
    const timetables = await get_timetables_db
      .get("/timetable")
      .json<TimetableInfos>();
    return Ok(timetables);
  } catch (error) {
    return Err("Something went wrong " + error);
  }
}

export async function get_timetable_by_id(
  access_token: string,
  timetable_id: string,
): Promise<Result<TimetableResponse, string>> {
  try {
    const get_timetables_id_db = create_ky_instance({
      authorised: true,
      unauthorizedCheck: true,
      auth_token: access_token,
    });
    const timetables = await get_timetables_id_db
      .get(`/timetable/${timetable_id}`)
      .json<TimetableResponse>();
    return Ok(timetables);
  } catch (error) {
    return Err("Something went wrong " + error);
  }
}

export async function reset_password(
  token_hash: string,
  password: string,
): Promise<Result<string, string>> {
  try {
    const reset_password_db = create_ky_instance({
      auth_token: "",
      authorised: false,
      unauthorizedCheck: false,
    }).extend({
      json: {
        tokenHash: token_hash,
        password: password,
      },
    });

    await reset_password_db.post("/auth/reset-password");
    return Ok("");
  } catch (error) {
    if (error instanceof HTTPError) {
      const errorResponse = error.data as ErrorResponse;
      const errorMessage = json_tryparse<ErrorInformation>(errorResponse.title);

      if (errorMessage.isOk()) {
        return Err(errorMessage.value.msg);
      }

      return Err(errorMessage.error);
    }
    return Err("Something went wrong " + error);
  }
}

export async function forgot_password(
  email: string,
): Promise<Result<string, string>> {
  try {
    const forgot_password_db = create_ky_instance({
      auth_token: "",
      authorised: false,
      unauthorizedCheck: false,
    }).extend({
      json: {
        email: email,
      },
    });
    await forgot_password_db.post("/auth/forgot-password");
    return Ok("");
  } catch (error) {
    if (error instanceof HTTPError) {
      const errorResponse = error.data as ErrorResponse;
      const errorMessage = json_tryparse<ErrorInformation>(errorResponse.title);

      if (errorMessage.isOk()) {
        return Err(errorMessage.value.msg);
      }

      return Err(errorMessage.error);
    }
    return Err("Something went wrong " + error);
  }
}

export async function put_timetable_by_id(
  access_token: string,
  timetable_id: string,
  timetable_data: TimetableResponse,
): Promise<Result<string, string>> {
  try {
    const put_timetable_id_db = create_ky_instance({
      authorised: true,
      unauthorizedCheck: true,
      auth_token: access_token,
    }).extend({
      json: timetable_data,
    });
    await put_timetable_id_db.put(`/timetable/${timetable_id}`);
    return Ok("");
  } catch (error) {
    return Err("Something went wrong " + error);
  }
}

export async function delete_timetable_by_id(
  access_token: string,
  timetable_id: string,
): Promise<Result<string, string>> {
  try {
    const delete_timetable_id_db = create_ky_instance({
      authorised: true,
      unauthorizedCheck: true,
      auth_token: access_token,
    });
    await delete_timetable_id_db.delete(`/timetable/${timetable_id}`);
    return Ok("");
  } catch (error) {
    return Err("Something went wrong " + error);
  }
}

export async function create_empty_timetable(
  access_token: string,
  timetable_name: string,
  semester: number,
  academic_year: string,
): Promise<Result<TimetableSummaryResponse, string>> {
  const timetable_post_template: TimetablePostTemplate = {
    academicYear: academic_year,
    metaData: [],
    name: timetable_name,
    semester: semester,
  };
  try {
    const create_empty_timetable_db = create_ky_instance({
      authorised: true,
      unauthorizedCheck: true,
      auth_token: access_token,
    }).extend({
      json: timetable_post_template,
    });
    const timetable_info = await create_empty_timetable_db
      .post("/timetable")
      .json<TimetableSummaryResponse>();
    return Ok(timetable_info);
  } catch (error) {
    return Err("Something went wrong " + error);
  }
}
