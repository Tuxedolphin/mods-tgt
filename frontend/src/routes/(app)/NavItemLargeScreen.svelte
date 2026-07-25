<script lang="ts">
  import { goto } from "$app/navigation";
  import { page } from "$app/state";
  import type { NavigationItemProp } from "$lib/types/internal";

  interface NavigationLargeItemInfo {
    information: NavigationItemProp;
  }
  let { information }: NavigationLargeItemInfo = $props();
  let selected = $derived(page.url.pathname.includes(information.path));
  const Icon = $derived(information.icon);
</script>

<!-- svelte-ignore a11y_click_events_have_key_events -->
<!-- svelte-ignore a11y_click_events_have_key_events -->
<!-- svelte-ignore a11y_no_static_element_interactions -->
<div
  onclick={() => {
    goto(information.path);
  }}
  class="flex cursor-pointer items-center justify-center md:justify-normal gap-1 {selected
    ? 'bg-base-300'
    : ''} rounded-4xl h-12 md:pl-4 md:mr-2 md:w-auto w-full"
>
  <Icon />
  <p class="hidden xl:block">{information.title}</p>
</div>
