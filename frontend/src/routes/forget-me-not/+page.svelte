<script lang="ts">
  import { onMount } from "svelte";
  import ModTogetherHero from "$lib/components/LoginPage/ModTogetherHero.svelte";
  import type { PageProps } from "./$types";

  let name = $state("");
  onMount(() => {
    const urlParams = new URLSearchParams(window.location.search);
    name = urlParams.get("user")!;
  });
</script>

<ModTogetherHero attempt_redirect={false}>
  <p>Your account has been successfully deleted.</p>

  <p class="italic pt-2">It was great to mods-together with you,</p>
  <p class="p-2">{name} 💕</p>
  <p class="italic">
    We will always be here if you need to mods together again.
  </p>
</ModTogetherHero>
