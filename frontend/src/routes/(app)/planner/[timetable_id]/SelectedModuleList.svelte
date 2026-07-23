<script lang="ts">
  import { currentlySelectedMods } from "$lib/shared/shared.svelte";
  import { getFullModInfo } from "$lib/utils/fetch_from_cache";
  import { removeModEntry } from "$lib/utils/format_db_information";
  import { X } from "@lucide/svelte";
  import { groupBy } from "es-toolkit";

  interface ModsSelectionComponentProps {
    acadYear: string;
    semester: number;
    timetable_id: string | undefined;
  }

  let mods_list = $derived(
    groupBy(
      $currentlySelectedMods.find((x) => x.id === timetable_id)?.metaData || [],
      (moduleCode) => moduleCode.moduleCode,
    ),
  );

  let { timetable_id, acadYear, semester }: ModsSelectionComponentProps =
    $props();
</script>

<!-- Mods List -->
<ul class="list bg-base-100 rounded-box shadow-md">
  {#each Object.entries(mods_list) as mods, i}
    <li class="list-row items-center">
      <div>
        <button class="btn {mods[1][0].colour} btn-square w-8 h-8"> </button>
      </div>
      <div class="list-col-grow">
        {#await getFullModInfo(mods[0], acadYear) then mod_info}
          <div>{mod_info.moduleCode}</div>
          <div class="text-xs uppercase font-semibold opacity-60">
            {mod_info.title}
          </div>
        {/await}
      </div>
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
        }}
      >
        <X></X>
      </button>
    </li>
  {/each}
</ul>
