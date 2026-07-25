<script lang="ts">
  import AvailableTimetableGrid from "$lib/components/AvailableTimetableGrid.svelte";
  import mods_tgt_byebye from "$lib/assets/mods_tgt_byebye.png?enhanced";
</script>

<p class="mt-2 text-xl font-semibold">Personal Planner</p>
<hr class="my-2 h-px border-0 bg-gray-200" />
<div class="flex items-center flex-col">
  <enhanced:img class="w-64" src={mods_tgt_byebye} alt="Mods Together Logo" />
  <div>
    Feature is under development. For now, you may use the Home tab to view your
    timetables!
  </div>
</div>
