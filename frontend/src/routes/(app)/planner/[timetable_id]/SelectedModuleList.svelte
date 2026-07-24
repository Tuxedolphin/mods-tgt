<script lang="ts">
  import {
    currentlySelectedMods,
    currentUserInformation,
  } from "$lib/shared/shared.svelte";
  import { roomHub } from "$lib/stores/roomHub";
  import type {
    RoomProfile,
    RoomRole,
    RoomVisibility,
  } from "$lib/types/db_raw_types";
  import { getFullModInfo } from "$lib/utils/fetch_from_cache";
  import { removeModEntry } from "$lib/utils/format_db_information";
  import { X } from "@lucide/svelte";
  import { groupBy } from "es-toolkit";
  import { get } from "svelte/store";

  interface ModsSelectionComponentProps {
    acadYear: string;
    semester: number;
    timetable_id: string | undefined;
    timetable_name: string;
    room_profiles: RoomProfile[];
    visibility: RoomVisibility;
    is_friend: boolean;
  }

  let mods_list = $derived(
    groupBy(
      $currentlySelectedMods.find((x) => x.id === timetable_id)?.metaData || [],
      (moduleCode) => moduleCode.moduleCode,
    ),
  );

  let {
    timetable_id,
    acadYear,
    semester,
    timetable_name,
    room_profiles,
    visibility,
    is_friend,
  }: ModsSelectionComponentProps = $props();

  let user_current_perms: "annon" | RoomRole = $derived(
    room_profiles.find((x) => x.userId === $currentUserInformation.userId)
      ?.role || "annon",
  );
</script>

<!-- Mods List -->
<ul
  class="{is_friend
    ? 'lg:grid-cols-3'
    : 'lg:grid-cols-1'} grid bg-base-100 rounded-box shadow-md"
>
  {#each Object.entries(mods_list) as mods, i}
    <li class="flex justify-between items-center p-4">
      <div class="flex gap-2 items-center">
        <div>
          <button class="btn {mods[1][0].colour} btn-square w-8 h-8"> </button>
        </div>
        <div>
          {#await getFullModInfo(mods[0], acadYear) then mod_info}
            <div>{mod_info.moduleCode}</div>
            <div class="text-xs uppercase font-semibold opacity-60">
              {mod_info.title}
            </div>
          {/await}
        </div>
      </div>

      {#if visibility === "publicEdit" || user_current_perms === "owner" || user_current_perms === "editor"}
        <button
          onclick={async () => {
            currentlySelectedMods.set(
              removeModEntry(
                $currentlySelectedMods,
                acadYear,
                semester,
                timetable_id!,
                mods[0],
              ),
            );

            const new_data = get(currentlySelectedMods).find(
              (x) => x.id === timetable_id,
            )!.metaData;

            await $roomHub?.invoke("UpdateTimetable", timetable_id, {
              Name: timetable_name,
              MetaData: new_data,
            });
          }}
        >
          <X></X>
        </button>
      {/if}
    </li>
  {/each}
</ul>
