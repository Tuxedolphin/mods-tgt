<script lang="ts">
  import UserAvatarComponent from "$lib/components/Profile/UserAvatarComponent.svelte";
  import { currentUserInformation } from "$lib/shared/shared.svelte";
  import type {
    Profile,
    RoomProfile,
    RoomRole,
    TimetableResponse,
  } from "$lib/types/db_raw_types";
  import { format_room_role_to_string } from "$lib/utils/frontend_utils";
  import { debounce } from "es-toolkit";
  import GenericDialog from "../../GenericDialog.svelte";
  import { roomHub } from "$lib/stores/roomHub";
  import { Check, ChevronDown, Edit2 } from "@lucide/svelte";

  let copy_text = $state("Copy Link!");
  let handle_name = $state("");
  let user_handle_results = $state([] as FrontendProfile[]);
  let popover_list = $state([] as HTMLUListElement[]);
  interface FrontendProfile extends Profile {
    loading: boolean;
  }

  interface ShareTimetableDialogProps {
    timetable_metadata: TimetableResponse;
    profiles: RoomProfile[];
    share_tt_dialog: HTMLDialogElement;
  }
  let {
    timetable_metadata,
    profiles,
    share_tt_dialog = $bindable(),
  }: ShareTimetableDialogProps = $props();

  async function change_member_role(
    handle: string,
    role: RoomRole,
    on_failure: () => void,
    on_success: (result: Profile[]) => void,
  ) {
    try {
      const result = (await $roomHub?.invoke(
        "SetMemberRole",
        handle,
        timetable_metadata.id,
        role,
      )) as Profile[];

      on_success(result);
    } catch {
      on_failure();
    }
  }

  async function remove_member(
    handle: string,
    on_failure: () => void,
    on_success: (result: Profile[]) => void,
  ) {
    try {
      const result = (await $roomHub?.invoke(
        "RevokeMemberAccess",
        handle,
        timetable_metadata.id,
      )) as Profile[];

      on_success(result);
    } catch {
      on_failure();
    }
  }

  const search_for_member = debounce(async (handle: string) => {
    const result = await $roomHub?.invoke(
      "FindUsersByHandle",
      handle,
      timetable_metadata.id,
    );
    user_handle_results = result;
    for (let i = 0; i < user_handle_results.length; i++) {
      const element = user_handle_results[i];
      element.loading = false;
    }
  }, 500);
</script>

<GenericDialog
  bind:dialog={share_tt_dialog}
  closeHandler={() => (copy_text = "Copy Link!")}
>
  <h1 class=" text-xl font-bold">Share "{timetable_metadata.name}"</h1>
  <fieldset class="fieldset">
    <label class="label" for="name">Search for users by their handle:</label>
    <input
      type="text"
      id="name"
      class="input"
      placeholder="Name"
      bind:value={handle_name}
      oninput={(new_text) => {
        search_for_member(handle_name);
      }}
    />
    <p class="label">
      Handle can be found at the top right corner. Your handle is @{$currentUserInformation.handle}
    </p>

    <ul class="list bg-base-100 rounded-box shadow-md overflow-auto max-h-28">
      {#each user_handle_results as profile}
        <li class="list-row">
          <div>
            <UserAvatarComponent user_info={profile}></UserAvatarComponent>
          </div>

          <div>
            <div>{profile.username}</div>
            <div class="text-xs font-semibold opacity-60">
              @{profile.handle}
            </div>
          </div>

          <div>
            <button
              class="btn {profile.loading ? 'btn-ghost' : ''}"
              onclick={async () => {
                profile.loading = true;
                change_member_role(
                  profile.userId,
                  "editor",
                  () => {
                    profile.loading = false;
                  },
                  (result) => {
                    profile.loading = false;
                    user_handle_results = [];
                    handle_name = "";
                  },
                );
              }}
            >
              {profile.loading ? "Inviting..." : "Invite"}
            </button>
          </div>
        </li>
      {/each}
    </ul>
  </fieldset>
  <h1 class=" font-bold">People with Access:</h1>
  <ul class="list bg-base-100 rounded-box shadow-md overflow-auto max-h-48">
    {#each profiles as profile, i}
      <li class="list-row">
        <div>
          <UserAvatarComponent user_info={profile}></UserAvatarComponent>
        </div>

        <div>
          <div>{profile.username}</div>
          <div class="text-xs font-semibold opacity-60">
            @{profile.handle}
          </div>
        </div>

        <div>
          <ul
            class="dropdown menu w-52 rounded-box bg-base-100 shadow-sm"
            popover
            id="popover-{profile.handle}"
            style="position-anchor:--anchor-{profile.handle}"
            bind:this={popover_list[i]}
          >
            {#each ["editor", "viewer"] as role}
              <li>
                <a
                  class="flex justify-between"
                  onclick={async () => {
                    change_member_role(
                      profile.userId,
                      role as RoomRole,
                      () => {
                        popover_list[i].hidePopover();
                      },
                      (result) => {
                        popover_list[i].hidePopover();
                        user_handle_results = [];
                        handle_name = "";
                      },
                    );
                  }}
                >
                  <div>{format_room_role_to_string(role as RoomRole)}</div>
                  {#if role === profile.role}
                    <Check></Check>
                  {/if}
                </a>
              </li>
            {/each}
            <hr class="my-2 h-px border-0 bg-gray-200" />

            <li>
              <a
                class="flex justify-between"
                onclick={async () => {
                  remove_member(
                    profile.userId,
                    () => {
                      popover_list[i].hidePopover();
                    },
                    (result) => {
                      popover_list[i].hidePopover();
                      user_handle_results = [];
                      handle_name = "";
                    },
                  );
                }}
              >
                <div>Remove</div>
              </a>
            </li>
          </ul>
          <div class="join btn-secondary">
            <button
              class="btn join-item {profile.role === 'owner'
                ? 'rounded-full'
                : 'rounded-l-full'}"
            >
              {format_room_role_to_string(profile.role)}</button
            >

            {#if profile.role !== "owner"}
              <button
                class="btn join-item rounded-r-full"
                popovertarget="popover-{profile.handle}"
                style="anchor-name:--anchor-{profile.handle}"
              >
                <ChevronDown size={12}></ChevronDown>
              </button>
            {/if}
          </div>
        </div>
      </li>
    {/each}
  </ul>

  <button
    class="btn w-full btn-primary"
    onclick={async () => {
      copy_text = "Link Copied!";
      await navigator.clipboard.writeText(window.location.href);
    }}>{copy_text}</button
  >
</GenericDialog>
