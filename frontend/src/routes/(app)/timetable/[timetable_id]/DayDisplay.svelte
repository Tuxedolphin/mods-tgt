<script lang="ts">
  let day_width = "w-16";
 
  let days = ["Mon", "Tue", "Wed", "Thu", "Fri"];
</script>

<div>
  <div class="h-6"></div>
  {#each days as day, i}
    {#if i % 2 == 0}
      <div class="bg-base-200 {day_width}">{day}</div>
    {:else}
      <div class="bg-base-100 {day_width}">{day}</div>
    {/if}
  {/each}
</div>
